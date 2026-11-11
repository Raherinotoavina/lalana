namespace Lalana.Domain.Identity;

/// <summary>A first or last name as shown on a profile: trimmed, 1 to 50 characters.</summary>
public static class PersonName
{
    public const int MaxLength = 50;

    public static bool TryNormalize(string? text, out string name)
    {
        name = string.Join(' ', (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return name.Length is > 0 and <= MaxLength;
    }
}
