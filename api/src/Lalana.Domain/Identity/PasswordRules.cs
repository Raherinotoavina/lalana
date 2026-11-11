namespace Lalana.Domain.Identity;

/// <summary>The password rules shown as a checklist under the field: 8 characters and a digit.</summary>
public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static bool HasMinLength(string password) => password.Length >= MinLength;

    public static bool HasDigit(string password) => password.Any(char.IsAsciiDigit);

    public static bool IsAcceptable(string? password) =>
        password is not null && HasMinLength(password) && HasDigit(password) && password.Length <= MaxLength;
}
