using Lalana.Application.Abstractions;
using Lalana.Domain.Identity;

namespace Lalana.Api.Auth;

/// <summary>Reads the member id from the validated access token's <c>sub</c> claim.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public UserId? Id =>
        UserId.TryParse(accessor.HttpContext?.User.FindFirst("sub")?.Value, out var id) ? id : null;
}
