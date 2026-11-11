using Lalana.Application.Auth;
using Lalana.Domain.Identity;
using Lalana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lalana.Infrastructure.Identity;

/// <summary><see cref="IAccounts"/> over ASP.NET Core Identity's <see cref="UserManager{TUser}"/>.</summary>
internal sealed class IdentityAccounts(UserManager<AppUser> users, AppDbContext db, TimeProvider clock) : IAccounts
{
    /// <summary>A real hash of a random password, so unknown identifiers cost a full verification.</summary>
    private static readonly Lazy<string> NobodysHash = new(() =>
        new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString()));

    public async Task<UserAccount?> FindByIdAsync(UserId id, CancellationToken cancellationToken) =>
        ToAccount(await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id.Value, cancellationToken));

    public async Task<UserAccount?> FindAsync(LoginIdentifier identifier, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        var value = identifier.Value;
        var normalizedEmail = users.NormalizeEmail(value);
        var user = identifier.Kind == IdentifierKind.Phone
            ? await query.SingleOrDefaultAsync(u => u.PhoneNumber == value, cancellationToken)
            : await query.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        return ToAccount(user);
    }

    public async Task<UserAccount?> CreateAsync(NewAccount account, string password, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var user = new AppUser
        {
            Id = id,
            UserName = id.ToString("N"),
            FirstName = account.FirstName,
            LastName = account.LastName,
            PhoneNumber = account.Phone?.E164,
            Email = account.Email?.Value,
            CreatedAt = clock.GetUtcNow(),
        };

        var result = await SaveAsync(() => users.CreateAsync(user, password));
        return result ? ToAccount(user) : null;
    }

    public async Task<PasswordCheck> CheckPasswordAsync(UserId id, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            CheckPasswordOfNobody(password);
            return PasswordCheck.Wrong;
        }

        if (await users.IsLockedOutAsync(user))
        {
            return PasswordCheck.LockedOut;
        }

        if (await users.CheckPasswordAsync(user, password))
        {
            await users.ResetAccessFailedCountAsync(user);
            return PasswordCheck.Valid;
        }

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? PasswordCheck.LockedOut : PasswordCheck.Wrong;
    }

    public void CheckPasswordOfNobody(string password) =>
        users.PasswordHasher.VerifyHashedPassword(new AppUser(), NobodysHash.Value, password);

    public async Task ConfirmAsync(UserId id, LoginIdentifier identifier, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("Unknown account.");
        if (identifier.Kind == IdentifierKind.Phone && user.PhoneNumber == identifier.Value)
        {
            user.PhoneNumberConfirmed = true;
        }
        else if (identifier.Kind == IdentifierKind.Email && user.NormalizedEmail == users.NormalizeEmail(identifier.Value))
        {
            user.EmailConfirmed = true;
        }
        else
        {
            return;
        }

        Ensure(await users.UpdateAsync(user));
    }

    public async Task<bool> ReplaceAsync(UserId id, LoginIdentifier identifier, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("Unknown account.");
        if (identifier.Kind == IdentifierKind.Phone)
        {
            user.PhoneNumber = identifier.Value;
            user.PhoneNumberConfirmed = true;
        }
        else
        {
            user.Email = identifier.Value;
            user.NormalizedEmail = users.NormalizeEmail(identifier.Value);
            user.EmailConfirmed = true;
        }

        return await SaveAsync(() => users.UpdateAsync(user));
    }

    public async Task SetPasswordAsync(UserId id, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new InvalidOperationException("Unknown account.");
        if (await users.HasPasswordAsync(user))
        {
            Ensure(await users.RemovePasswordAsync(user));
        }

        Ensure(await users.AddPasswordAsync(user, password));
        Ensure(await users.SetLockoutEndDateAsync(user, null));
        Ensure(await users.ResetAccessFailedCountAsync(user));
    }

    /// <summary>Runs a save; false when the phone or e-mail is already used by another account.</summary>
    private static async Task<bool> SaveAsync(Func<Task<IdentityResult>> save)
    {
        try
        {
            Ensure(await save());
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Identity refused the change: " + string.Join(", ", result.Errors.Select(e => e.Code)));
        }
    }

    private static UserAccount? ToAccount(AppUser? user) =>
        user is null
            ? null
            : new UserAccount(
                UserId.From(user.Id),
                user.PhoneNumber is { } phone ? PhoneNumber.Parse(phone) : null,
                user.PhoneNumberConfirmed,
                user.Email is { } email ? EmailAddress.Parse(email) : null,
                user.EmailConfirmed,
                user.FirstName,
                user.LastName);
}
