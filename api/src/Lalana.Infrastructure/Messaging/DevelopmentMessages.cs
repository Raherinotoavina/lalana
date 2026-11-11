using System.Collections.Concurrent;
using Lalana.Application.Messaging;
using Lalana.Domain.Identity;

namespace Lalana.Infrastructure.Messaging;

/// <summary>A message the development sender kept instead of sending.</summary>
/// <param name="To">E.164 number or e-mail address.</param>
/// <param name="Subject">E-mail subject, null for an SMS.</param>
/// <param name="Text">The message body.</param>
/// <param name="SentAt">When it was "sent".</param>
public sealed record DevelopmentMessage(string To, string? Subject, string Text, DateTimeOffset SentAt);

/// <summary>
/// Stand-in for the SMS and e-mail providers in Development and tests: keeps the last messages in
/// memory (never in logs) so a developer or a test can read the code.
/// </summary>
public sealed class DevelopmentMessages(TimeProvider clock) : ISmsSender, IEmailSender
{
    private const int Capacity = 200;
    private readonly ConcurrentQueue<DevelopmentMessage> messages = new();

    public IReadOnlyList<DevelopmentMessage> All => [.. messages];

    public DevelopmentMessage? LastTo(string to) => All.LastOrDefault(m => m.To == to);

    public Task SendAsync(PhoneNumber to, string text, CancellationToken cancellationToken) =>
        Keep(new DevelopmentMessage(to.E164, null, text, clock.GetUtcNow()));

    public Task SendAsync(EmailAddress to, string subject, string text, CancellationToken cancellationToken) =>
        Keep(new DevelopmentMessage(to.Value, subject, text, clock.GetUtcNow()));

    private Task Keep(DevelopmentMessage message)
    {
        messages.Enqueue(message);
        while (messages.Count > Capacity)
        {
            messages.TryDequeue(out _);
        }

        return Task.CompletedTask;
    }
}
