namespace Lalana.Domain.Identity;

/// <summary>
/// A Malagasy mobile number in E.164 form (<c>+2613XXXXXXXX</c>). Accepts the local forms
/// people type (<c>034 12 345 67</c>, <c>34 12 345 67</c>) and international ones
/// (<c>+261 34…</c>, <c>00261 34…</c>), and only the mobile prefixes 032, 033, 034, 037 and 038.
/// </summary>
public sealed record PhoneNumber
{
    public const string CountryCode = "+261";

    private static readonly char[] Separators = [' ', '.', '-', '(', ')', ' '];

    private PhoneNumber(string subscriber) => Subscriber = subscriber;

    /// <summary>The nine digits after the country code, for example <c>341234567</c>.</summary>
    public string Subscriber { get; }

    /// <summary>The E.164 form, for example <c>+261341234567</c>.</summary>
    public string E164 => CountryCode + Subscriber;

    /// <summary>The local form people read, for example <c>034 12 345 67</c>.</summary>
    public string Local => $"0{Subscriber[..2]} {Subscriber[2..4]} {Subscriber[4..7]} {Subscriber[7..]}";

    public static bool TryParse(string? text, out PhoneNumber phone)
    {
        phone = null!;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var compact = string.Concat(text.Trim().Split(Separators, StringSplitOptions.RemoveEmptyEntries));
        var subscriber = compact switch
        {
            _ when compact.StartsWith("+261", StringComparison.Ordinal) => compact[4..],
            _ when compact.StartsWith("00261", StringComparison.Ordinal) => compact[5..],
            _ when compact.StartsWith("261", StringComparison.Ordinal) && compact.Length == 12 => compact[3..],
            _ when compact.StartsWith('0') && compact.Length == 10 => compact[1..],
            _ => compact,
        };

        if (!IsMobileSubscriber(subscriber))
        {
            return false;
        }

        phone = new PhoneNumber(subscriber);
        return true;
    }

    public static PhoneNumber Parse(string text) =>
        TryParse(text, out var phone) ? phone : throw new FormatException("Not a Malagasy mobile number.");

    /// <summary>Hides the middle digits for logs and messages: <c>+261 34 ** *** 67</c>.</summary>
    public string Masked => $"{CountryCode} {Subscriber[..2]} ** *** {Subscriber[7..]}";

    public override string ToString() => E164;

    private static bool IsMobileSubscriber(string subscriber) =>
        subscriber.Length == 9
        && subscriber.All(char.IsAsciiDigit)
        && subscriber[0] == '3'
        && subscriber[1] is '2' or '3' or '4' or '7' or '8';
}
