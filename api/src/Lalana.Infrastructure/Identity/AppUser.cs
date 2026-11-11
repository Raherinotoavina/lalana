using Microsoft.AspNetCore.Identity;

namespace Lalana.Infrastructure.Identity;

/// <summary>
/// The ASP.NET Core Identity user. <c>PhoneNumber</c> holds the E.164 number, <c>Email</c> the
/// lower-cased address; the user name is the id, since members sign in with either of them.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
