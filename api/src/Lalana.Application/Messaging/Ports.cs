using Lalana.Domain.Identity;

namespace Lalana.Application.Messaging;

/// <summary>Sends SMS to Malagasy mobile numbers (provider chosen in Phase 03).</summary>
public interface ISmsSender
{
    Task SendAsync(PhoneNumber to, string text, CancellationToken cancellationToken);
}

/// <summary>Sends transactional e-mail (provider chosen in Phase 03).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailAddress to, string subject, string text, CancellationToken cancellationToken);
}
