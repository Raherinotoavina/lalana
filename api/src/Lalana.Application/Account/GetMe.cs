using Lalana.Application.Abstractions;
using Lalana.Application.Auth;
using Lalana.Application.Profiles;

namespace Lalana.Application.Account;

/// <summary>What the API tells a member about their own account.</summary>
/// <param name="Id">The member's id.</param>
/// <param name="FirstName">From the profile, or from sign-up before the profile exists.</param>
/// <param name="LastName">Same as <paramref name="FirstName"/>.</param>
/// <param name="Phone">E.164 phone number, if any.</param>
/// <param name="PhoneConfirmed">Whether the phone was confirmed by a code.</param>
/// <param name="Email">E-mail address, if any.</param>
/// <param name="EmailConfirmed">Whether the e-mail was confirmed by a code.</param>
/// <param name="HasProfile">False until the profile is created (bootstrap).</param>
public sealed record MeResult(
    Guid Id,
    string FirstName,
    string LastName,
    string? Phone,
    bool PhoneConfirmed,
    string? Email,
    bool EmailConfirmed,
    bool HasProfile);

/// <summary>Returns the caller's account, or null when the request carries no valid identity.</summary>
public sealed class GetMeHandler(ICurrentUser currentUser, IAccounts accounts, IProfiles profiles)
{
    public async Task<MeResult?> HandleAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } id || await accounts.FindByIdAsync(id, cancellationToken) is not { } account)
        {
            return null;
        }

        var profile = await profiles.FindAsync(id, cancellationToken);
        return new MeResult(
            id.Value,
            profile?.FirstName ?? account.FirstName,
            profile?.LastName ?? account.LastName,
            account.Phone?.E164,
            account.PhoneConfirmed,
            account.Email?.Value,
            account.EmailConfirmed,
            profile is not null);
    }
}
