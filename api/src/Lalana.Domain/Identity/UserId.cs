namespace Lalana.Domain.Identity;

/// <summary>Identifier of a member: the account's GUID, never empty.</summary>
public readonly record struct UserId
{
    private UserId(Guid value) => Value = value;

    public Guid Value { get; }

    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("A user id cannot be empty.", nameof(value))
            : new UserId(value);

    public static bool TryParse(string? text, out UserId id)
    {
        if (Guid.TryParse(text, out var value) && value != Guid.Empty)
        {
            id = new UserId(value);
            return true;
        }

        id = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
