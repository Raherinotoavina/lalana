namespace Lalana.Domain.Identity;

/// <summary>Which kind of identifier a member typed.</summary>
public enum IdentifierKind
{
    Phone,
    Email,
}

/// <summary>
/// What a member types in "Téléphone ou email": a value containing <c>@</c> is an e-mail,
/// anything else a Malagasy mobile number.
/// </summary>
public sealed record LoginIdentifier
{
    private LoginIdentifier(PhoneNumber? phone, EmailAddress? email)
    {
        Phone = phone;
        Email = email;
    }

    public PhoneNumber? Phone { get; }

    public EmailAddress? Email { get; }

    public IdentifierKind Kind => Phone is null ? IdentifierKind.Email : IdentifierKind.Phone;

    /// <summary>The normalized value: E.164 for a phone, the lower-cased address for an e-mail.</summary>
    public string Value => Phone?.E164 ?? Email!.Value;

    public string Masked => Phone?.Masked ?? Email!.Masked;

    public static LoginIdentifier Of(PhoneNumber phone) => new(phone, null);

    public static LoginIdentifier Of(EmailAddress email) => new(null, email);

    /// <summary>Parses the value, or explains why it cannot be one.</summary>
    public static IdentifierParseResult Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new IdentifierParseResult(null, IdentifierError.Missing);
        }

        if (text.Contains('@', StringComparison.Ordinal))
        {
            return EmailAddress.TryParse(text, out var email)
                ? new IdentifierParseResult(Of(email), null)
                : new IdentifierParseResult(null, IdentifierError.InvalidEmail);
        }

        return PhoneNumber.TryParse(text, out var phone)
            ? new IdentifierParseResult(Of(phone), null)
            : new IdentifierParseResult(null, IdentifierError.InvalidPhone);
    }

    public override string ToString() => Masked;
}

public enum IdentifierError
{
    Missing,
    InvalidPhone,
    InvalidEmail,
}

/// <param name="Identifier">The parsed identifier, when the text is valid.</param>
/// <param name="Error">Why the text is not an identifier, otherwise.</param>
public sealed record IdentifierParseResult(LoginIdentifier? Identifier, IdentifierError? Error);
