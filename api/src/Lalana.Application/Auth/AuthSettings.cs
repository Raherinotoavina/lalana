namespace Lalana.Application.Auth;

/// <summary>Lifetimes and limits of codes and sessions (section <c>Auth</c>).</summary>
public sealed class AuthSettings
{
    public TimeSpan CodeLifetime { get; set; } = TimeSpan.FromMinutes(10);

    public int CodeMaxAttempts { get; set; } = 5;

    /// <summary>Minimum time between two codes of the same purpose sent to the same phone or e-mail.</summary>
    public TimeSpan CodeResendDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Codes allowed per phone or e-mail per hour, all purposes together.</summary>
    public int CodesPerHour { get; set; } = 5;

    public TimeSpan ResetTicketLifetime { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}
