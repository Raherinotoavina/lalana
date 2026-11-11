using Lalana.Domain.Identity;

namespace Lalana.Domain.Profiles;

/// <summary>A member's public profile, one per account, created on first use.</summary>
public sealed class Profile
{
    private Profile(UserId id, string firstName, string lastName, DateTimeOffset createdAt)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = createdAt;
    }

    public UserId Id { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public DateTimeOffset CreatedAt { get; }

    /// <summary>Creates a profile; both names must be valid (see <see cref="PersonName"/>).</summary>
    public static Profile Create(UserId id, string firstName, string lastName, DateTimeOffset createdAt)
    {
        if (!PersonName.TryNormalize(firstName, out var first) || !PersonName.TryNormalize(lastName, out var last))
        {
            throw new ArgumentException("A profile needs a first and a last name.");
        }

        return new Profile(id, first, last, createdAt);
    }

    /// <summary>Rebuilds a stored profile.</summary>
    public static Profile Restore(UserId id, string firstName, string lastName, DateTimeOffset createdAt) =>
        new(id, firstName, lastName, createdAt);
}
