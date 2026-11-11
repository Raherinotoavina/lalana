using Lalana.Application.Auth;
using Lalana.Application.Tests.Fakes;
using Lalana.Domain.Identity;
using Shouldly;

namespace Lalana.Application.Tests.Auth;

public sealed class OneTimeCodesTests
{
    private readonly AuthWorld world = new();
    private static readonly LoginIdentifier Phone = LoginIdentifier.Of(PhoneNumber.Parse("034 12 345 67"));
    private static readonly LoginIdentifier Email = LoginIdentifier.Of(EmailAddress.Parse("sitraka@exemple.mg"));

    [Fact]
    public async Task Sends_a_six_digit_code_by_sms_and_stores_only_its_hash()
    {
        var sent = (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Value!;

        world.Sender.Sent.ShouldHaveSingleItem().To.ShouldBe("+261341234567");
        world.Sender.LastCode.Length.ShouldBe(6);
        world.CodeStore.Codes.ShouldHaveSingleItem().Hash.ShouldBe("h:" + world.Sender.LastCode);
        sent.Channel.ShouldBe(IdentifierKind.Phone);
        sent.MaskedDestination.ShouldBe("+261 34 ** *** 67");
        sent.ExpiresAt.ShouldBe(world.Clock.GetUtcNow() + world.Settings.CodeLifetime);
    }

    [Fact]
    public async Task Sends_a_reset_code_by_email_with_its_own_wording()
    {
        await world.Codes.SendAsync(CodePurpose.ResetPassword, Email, null, world.Cancel);

        world.Sender.Sent.ShouldHaveSingleItem().Text.ShouldContain("nouveau mot de passe");
    }

    [Fact]
    public async Task Refuses_a_second_code_before_the_resend_delay()
    {
        await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel);
        world.Clock.Advance(TimeSpan.FromSeconds(20));

        var second = await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel);

        second.Failure.ShouldBe(new AuthFailure(AuthError.CodeRateLimited, TimeSpan.FromSeconds(40)));
    }

    [Fact]
    public async Task The_resend_delay_applies_per_purpose()
    {
        await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel);

        (await world.Codes.SendAsync(CodePurpose.ResetPassword, Phone, null, world.Cancel)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Refuses_more_than_the_hourly_limit_for_one_destination()
    {
        for (var i = 0; i < world.Settings.CodesPerHour; i++)
        {
            (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Succeeded.ShouldBeTrue();
            world.Wait();
        }

        var refused = await world.Codes.SendAsync(CodePurpose.ResetPassword, Phone, null, world.Cancel);

        refused.Failure!.Error.ShouldBe(AuthError.CodeRateLimited);
        refused.Failure.RetryAfter.ShouldBe(TimeSpan.FromMinutes(55));
    }

    [Fact]
    public async Task Accepts_the_right_code_once()
    {
        var sent = (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Value!;
        var code = world.Sender.LastCode;

        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.Confirm, $" {code} ", world.Cancel)).Succeeded.ShouldBeTrue();
        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.Confirm, code, world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }

    [Fact]
    public async Task A_new_code_retires_the_previous_one()
    {
        var first = (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Value!;
        var firstCode = world.Sender.LastCode;
        world.Wait();
        await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel);

        (await world.Codes.VerifyAsync(first.Id, CodePurpose.Confirm, firstCode, world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }

    [Fact]
    public async Task Refuses_an_expired_code()
    {
        var sent = (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Value!;
        world.Clock.Advance(world.Settings.CodeLifetime);

        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.Confirm, world.Sender.LastCode, world.Cancel))
            .Failure!.Error.ShouldBe(AuthError.CodeExpired);
    }

    [Fact]
    public async Task Locks_the_code_after_too_many_wrong_attempts()
    {
        var sent = (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Value!;
        var errors = new List<AuthError>();
        for (var i = 0; i < world.Settings.CodeMaxAttempts; i++)
        {
            errors.Add((await world.Codes.VerifyAsync(sent.Id, CodePurpose.Confirm, "000000x", world.Cancel)).Failure!.Error);
        }

        errors[..^1].ShouldAllBe(e => e == AuthError.CodeInvalid);
        errors[^1].ShouldBe(AuthError.CodeAttemptsExceeded);
        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.Confirm, world.Sender.LastCode, world.Cancel))
            .Failure!.Error.ShouldBe(AuthError.CodeAttemptsExceeded);
    }

    [Fact]
    public async Task Refuses_a_code_used_for_another_purpose_owner_or_missing()
    {
        var owner = UserId.From(Guid.NewGuid());
        var sent = (await world.Codes.SendAsync(CodePurpose.ChangeIdentifier, Phone, owner, world.Cancel)).Value!;
        var code = world.Sender.LastCode;

        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.Confirm, code, world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.ChangeIdentifier, code, world.Cancel, UserId.From(Guid.NewGuid())))
            .Failure!.Error.ShouldBe(AuthError.CodeInvalid);
        (await world.Codes.VerifyAsync(Guid.NewGuid(), CodePurpose.Confirm, code, world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.ChangeIdentifier, null, world.Cancel, owner)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
        (await world.Codes.VerifyAsync(sent.Id, CodePurpose.ChangeIdentifier, code, world.Cancel, owner)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Pretends_to_send_without_storing_or_sending()
    {
        var pretended = world.Codes.Pretend(Email);

        pretended.MaskedDestination.ShouldBe("s***@exemple.mg");
        world.CodeStore.Codes.ShouldBeEmpty();
        world.Sender.Sent.ShouldBeEmpty();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Redeems_a_ticket_once_and_refuses_bad_ones()
    {
        var owner = UserId.From(Guid.NewGuid());
        var (ticket, _) = await world.Codes.IssueTicketAsync(owner, Phone.Value, world.Cancel);

        (await world.Codes.RedeemTicketAsync(ticket, world.Cancel)).ShouldBe(owner);
        (await world.Codes.RedeemTicketAsync(ticket, world.Cancel)).ShouldBeNull();
        (await world.Codes.RedeemTicketAsync(null, world.Cancel)).ShouldBeNull();
        (await world.Codes.RedeemTicketAsync("no-dot", world.Cancel)).ShouldBeNull();
        (await world.Codes.RedeemTicketAsync($"{Guid.NewGuid():N}.secret", world.Cancel)).ShouldBeNull();

        var (other, _) = await world.Codes.IssueTicketAsync(owner, Phone.Value, world.Cancel);
        (await world.Codes.RedeemTicketAsync(other.Split('.')[0] + ".forged", world.Cancel)).ShouldBeNull();
        world.Clock.Advance(world.Settings.ResetTicketLifetime);
        (await world.Codes.RedeemTicketAsync(other, world.Cancel)).ShouldBeNull();
    }

    [Fact]
    public async Task Tickets_do_not_count_towards_the_code_limits()
    {
        await world.Codes.IssueTicketAsync(UserId.From(Guid.NewGuid()), Phone.Value, world.Cancel);

        (await world.Codes.SendAsync(CodePurpose.Confirm, Phone, null, world.Cancel)).Succeeded.ShouldBeTrue();
    }
}
