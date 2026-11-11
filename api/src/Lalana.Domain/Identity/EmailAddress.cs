namespace Lalana.Domain.Identity;

/// <summary>An e-mail address, trimmed and lower-cased, with one <c>@</c> and a dotted domain.</summary>
public sealed record EmailAddress
{
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? text, out EmailAddress email)
    {
        email = null!;
        var value = text?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = value.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != value.LastIndexOf('@'))
        {
            return false;
        }

        var domain = value[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        if (dot <= 0 || dot == domain.Length - 1 || domain.StartsWith('.') || domain.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        email = new EmailAddress(value);
        return true;
    }

    public static EmailAddress Parse(string text) =>
        TryParse(text, out var email) ? email : throw new FormatException("Not an e-mail address.");

    /// <summary>Keeps the first letter and the domain: <c>s***@exemple.com</c>.</summary>
    public string Masked => $"{Value[0]}***{Value[Value.IndexOf('@', StringComparison.Ordinal)..]}";

    public override string ToString() => Value;
}
