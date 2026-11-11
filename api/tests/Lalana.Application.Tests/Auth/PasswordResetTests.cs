using Lalana.Application.Auth;
using Lalana.Application.Tests.Fakes;
using Shouldly;

namespace Lalana.Application.Tests.Auth;

public sealed class PasswordResetTests
{
    private readonly AuthWorld world = new();

    [Fact]
    public async Task Resets_the_password_from_a_phone_and_ends_other_sessions()
    {
        var account = world.Accounts.Add(phone: "+261341234567");
        var oldSession = await world.Sessions.StartAsync(account.Id, world.Cancel);

        var sent = (await world.Reset.RequestAsync("034 12 345 67", world.Cancel)).Value!;
        var ticket = (await world.Reset.VerifyCodeAsync(sent.Id, world.Sender.LastCode, world.Cancel)).Value!;
        var session = (await world.Reset.ResetAsync(ticket.Ticket, "nouveau2026", world.Cancel)).Value!;

        session.UserId.ShouldBe(account.Id);
        world.Accounts.PasswordOf(account.Id).ShouldBe("nouveau2026");
        (await world.Sessions.RefreshAsync(oldSession.RefreshToken, world.Cancel)).Succeeded.ShouldBeFalse();
        (await world.Reset.ResetAsync(ticket.Ticket, "encore2026", world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidResetTicket);
    }

    [Fact]
    public async Task Answers_the_same_way_for_unknown_or_unconfirmed_identifiers()
    {
        world.Accounts.Add(email: "pas-confirme@exemple.mg", confirmed: false);

        var unknown = (await world.Reset.RequestAsync("0381234567", world.Cancel)).Value!;
        var unconfirmed = (await world.Reset.RequestAsync("pas-confirme@exemple.mg", world.Cancel)).Value!;

        unknown.MaskedDestination.ShouldBe("+261 38 ** *** 67");
        unconfirmed.MaskedDestination.ShouldBe("p***@exemple.mg");
        world.Sender.Sent.ShouldBeEmpty();
        (await world.Reset.VerifyCodeAsync(unknown.Id, "123456", world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }

    [Fact]
    public async Task Hides_the_rate_limit_behind_the_usual_answer()
    {
        world.Accounts.Add(phone: "+261341234567");
        await world.Reset.RequestAsync("0341234567", world.Cancel);

        var limited = await world.Reset.RequestAsync("0341234567", world.Cancel);

        limited.Succeeded.ShouldBeTrue();
        limited.Value!.ResendAvailableAt.ShouldBe(world.Clock.GetUtcNow() + world.Settings.CodeResendDelay);
        world.Sender.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Refuses_malformed_identifiers_weak_passwords_and_bad_tickets()
    {
        (await world.Reset.RequestAsync("12", world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidPhone);
        (await world.Reset.ResetAsync("x.y", "court", world.Cancel)).Failure!.Error.ShouldBe(AuthError.WeakPassword);
        (await world.Reset.ResetAsync("x.y", "nouveau2026", world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidResetTicket);
        (await world.Reset.VerifyCodeAsync(Guid.NewGuid(), "123456", world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }

    [Fact]
    public async Task Refuses_a_ticket_whose_account_is_gone()
    {
        var account = world.Accounts.Add(phone: "+261341234567");
        var sent = (await world.Reset.RequestAsync("0341234567", world.Cancel)).Value!;
        var ticket = (await world.Reset.VerifyCodeAsync(sent.Id, world.Sender.LastCode, world.Cancel)).Value!;
        world.Accounts.Remove(account.Id);

        (await world.Reset.ResetAsync(ticket.Ticket, "nouveau2026", world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidResetTicket);
    }
}
