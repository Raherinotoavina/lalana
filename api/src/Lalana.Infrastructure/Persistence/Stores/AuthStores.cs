using Lalana.Application.Auth;
using Lalana.Application.Profiles;
using Lalana.Domain.Identity;
using Lalana.Domain.Profiles;
using Lalana.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lalana.Infrastructure.Persistence.Stores;

internal sealed class EfVerificationCodes(AppDbContext db) : IVerificationCodes
{
    public Task<StoredCode?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.VerificationCodes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<(CodePurpose Purpose, DateTimeOffset SentAt)>> SentSinceAsync(
        string destination,
        DateTimeOffset since,
        CancellationToken cancellationToken) =>
        (await db.VerificationCodes
            .Where(c => c.Destination == destination && c.CreatedAt > since && c.Purpose != CodePurpose.ResetTicket)
            .Select(c => new { c.Purpose, c.CreatedAt })
            .ToListAsync(cancellationToken))
        .Select(c => (c.Purpose, c.CreatedAt))
        .ToList();

    public async Task AddAsync(StoredCode code, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.VerificationCodes
            .Where(c => c.Destination == code.Destination && c.Purpose == code.Purpose && c.ConsumedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.ConsumedAt, code.CreatedAt), cancellationToken);
        db.VerificationCodes.Add(code);
        await db.SaveChangesAsync(cancellationToken);
        db.Entry(code).State = EntityState.Detached;
        await transaction.CommitAsync(cancellationToken);
    }

    public Task RecordFailedAttemptAsync(Guid id, CancellationToken cancellationToken) =>
        db.VerificationCodes
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.FailedAttempts, c => c.FailedAttempts + 1), cancellationToken);

    public Task ConsumeAsync(Guid id, DateTimeOffset at, CancellationToken cancellationToken) =>
        db.VerificationCodes
            .Where(c => c.Id == id && c.ConsumedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.ConsumedAt, at), cancellationToken);
}

internal sealed class EfRefreshTokens(AppDbContext db) : IRefreshTokens
{
    public Task<StoredRefreshToken?> FindByHashAsync(string hash, CancellationToken cancellationToken) =>
        db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.Hash == hash, cancellationToken);

    public async Task AddAsync(StoredRefreshToken token, CancellationToken cancellationToken)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        db.Entry(token).State = EntityState.Detached;
    }

    public async Task<bool> TryRotateAsync(Guid id, Guid replacedById, DateTimeOffset at, CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(t => t.Id == id && t.ReplacedById == null && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                set => set.SetProperty(t => t.ReplacedById, replacedById).SetProperty(t => t.RevokedAt, at),
                cancellationToken) == 1;

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset at, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, at), cancellationToken);

    public Task RevokeAllAsync(UserId userId, DateTimeOffset at, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId.Value && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, at), cancellationToken);
}

internal sealed class EfProfiles(AppDbContext db) : IProfiles
{
    public async Task<Profile?> FindAsync(UserId id, CancellationToken cancellationToken) =>
        ToProfile(await db.Profiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id.Value, cancellationToken));

    public async Task<(Profile Profile, bool Created)> AddOrGetAsync(Profile profile, CancellationToken cancellationToken)
    {
        var row = new ProfileRow
        {
            Id = profile.Id.Value,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            CreatedAt = profile.CreatedAt,
        };
        db.Profiles.Add(row);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return (profile, true);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two bootstrap calls raced: the other one created it.
            db.Entry(row).State = EntityState.Detached;
            return ((await FindAsync(profile.Id, cancellationToken))!, false);
        }
        finally
        {
            db.Entry(row).State = EntityState.Detached;
        }
    }

    private static Profile? ToProfile(ProfileRow? row) =>
        row is null ? null : Profile.Restore(UserId.From(row.Id), row.FirstName, row.LastName, row.CreatedAt);
}
