using Lalana.Application.Abstractions;

namespace Lalana.Application.Account;

/// <summary>What the API tells a member about their own account.</summary>
/// <param name="Id">The member's id.</param>
public sealed record MeResult(Guid Id);

/// <summary>Returns the caller's account, or null when the request carries no valid identity.</summary>
public sealed class GetMeHandler(ICurrentUser currentUser)
{
    public MeResult? Handle() => currentUser.Id is { } id ? new MeResult(id.Value) : null;
}
