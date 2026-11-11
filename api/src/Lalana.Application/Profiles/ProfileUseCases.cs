using Lalana.Application.Auth;
using Lalana.Domain.Identity;
using Lalana.Domain.Profiles;

namespace Lalana.Application.Profiles;

public interface IProfiles
{
    Task<Profile?> FindAsync(UserId id, CancellationToken cancellationToken);

    /// <summary>Adds the profile, or returns the one already stored for this member.</summary>
    Task<(Profile Profile, bool Created)> AddOrGetAsync(Profile profile, CancellationToken cancellationToken);
}

/// <summary>Creates the caller's profile on first use; calling it again returns the same profile.</summary>
public sealed class BootstrapProfile(IAccounts accounts, IProfiles profiles, TimeProvider clock)
{
    /// <returns>The profile and whether this call created it.</returns>
    public async Task<AuthResult<(Profile Profile, bool Created)>> HandleAsync(
        UserId userId,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        if (await profiles.FindAsync(userId, cancellationToken) is { } existing)
        {
            return (existing, false);
        }

        if (await accounts.FindByIdAsync(userId, cancellationToken) is not { } account)
        {
            return AuthError.AccountNotFound;
        }

        var first = string.IsNullOrWhiteSpace(firstName) ? account.FirstName : firstName;
        var last = string.IsNullOrWhiteSpace(lastName) ? account.LastName : lastName;
        if (!PersonName.TryNormalize(first, out first) || !PersonName.TryNormalize(last, out last))
        {
            return AuthError.NameRequired;
        }

        return await profiles.AddOrGetAsync(Profile.Create(userId, first, last, clock.GetUtcNow()), cancellationToken);
    }
}
