using Lalana.Application.Auth;
using Lalana.Application.Tests.Fakes;
using Lalana.Domain.Identity;
using Shouldly;

namespace Lalana.Application.Tests.Auth;

public sealed class AccountAuthTests
{
    private readonly AuthWorld world = new();

    private Task<AuthResult<SentCode>> SignUp(string? phone = "034 12 345 67", string? email = null, string? password = "sitraka2026", string? first = "Sitraka", string? last = "Rabe") =>
        world.Auth.SignUpAsync(new SignUpCommand(first, last, phone, email, password), world.Cancel);

    [Fact]
    public async Task Signs_up_with_a_phone_and_sends_an_sms_code()
    {
        var sent = (await SignUp()).Value!;

        sent.Channel.ShouldBe(IdentifierKind.Phone);
        world.Sender.Sent.ShouldHaveSingleItem().To.ShouldBe("+261341234567");
    }

    [Fact]
    public async Task Signs_up_with_both_and_confirms_the_phone_first()
    {
        var sent = (await SignUp(email: "Sitraka@Exemple.mg")).Value!;

        sent.Channel.ShouldBe(IdentifierKind.Phone);
        var session = (await world.Auth.ConfirmAsync(sent.Id, world.Sender.LastCode, world.Cancel)).Value!;
        var account = world.Accounts[session.UserId];
        account.PhoneConfirmed.ShouldBeTrue();
        account.EmailConfirmed.ShouldBeFalse();
        account.Email!.Value.ShouldBe("sitraka@exemple.mg");
    }

    [Fact]
    public async Task Signs_up_with_an_email_only()
    {
        var sent = (await SignUp(phone: null, email: "sitraka@exemple.mg")).Value!;

        sent.Channel.ShouldBe(IdentifierKind.Email);
        world.Sender.Sent.ShouldHaveSingleItem().To.ShouldBe("sitraka@exemple.mg");
    }

    public static TheoryData<string?, string?, string?, string?, AuthError> Refusals => new()
    {
        { null, null, "sitraka2026", "Rabe", AuthError.IdentifierRequired },
        { "  ", "", "sitraka2026", "Rabe", AuthError.IdentifierRequired },
        { "034 12 34", null, "sitraka2026", "Rabe", AuthError.InvalidPhone },
        { null, "pas-un-email", "sitraka2026", "Rabe", AuthError.InvalidEmail },
        { "0341234567", null, "rabesitraka", "Rabe", AuthError.WeakPassword },
        { "0341234567", null, "sitraka2026", " ", AuthError.NameRequired },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task Refuses_invalid_sign_ups(string? phone, string? email, string? password, string? last, AuthError error)
    {
        (await SignUp(phone, email, password, last: last)).Failure!.Error.ShouldBe(error);
        world.Sender.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Refuses_a_phone_or_email_already_used()
    {
        world.Accounts.Add(phone: "+261341234567", email: "pris@exemple.mg");

        (await SignUp(phone: "034 12 345 67")).Failure!.Error.ShouldBe(AuthError.PhoneTaken);
        (await SignUp(phone: null, email: "PRIS@exemple.mg")).Failure!.Error.ShouldBe(AuthError.EmailTaken);
    }

    [Fact]
    public async Task Reports_a_conflict_found_only_when_saving()
    {
        world.Accounts.FailNextCreate = true;
        (await SignUp()).Failure!.Error.ShouldBe(AuthError.PhoneTaken);

        world.Accounts.FailNextCreate = true;
        (await SignUp(phone: null, email: "a@b.mg")).Failure!.Error.ShouldBe(AuthError.EmailTaken);
    }

    [Fact]
    public async Task Confirming_refuses_a_code_whose_account_is_gone()
    {
        var sent = (await SignUp()).Value!;
        var account = await world.Accounts.FindAsync(LoginIdentifier.Of(PhoneNumber.Parse("0341234567")), world.Cancel);
        world.Accounts.Remove(account!.Id);

        (await world.Auth.ConfirmAsync(sent.Id, world.Sender.LastCode, world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }

    [Fact]
    public async Task Confirming_reports_a_wrong_code()
    {
        var sent = (await SignUp()).Value!;

        (await world.Auth.ConfirmAsync(sent.Id, "999999x", world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }

    [Fact]
    public async Task Signs_in_with_a_confirmed_phone_typed_locally_or_the_email()
    {
        var account = world.Accounts.Add(phone: "+261341234567", email: "sitraka@exemple.mg");

        (await world.Auth.SignInAsync("034 12 345 67", "motdepasse1", world.Cancel)).Value!.UserId.ShouldBe(account.Id);
        (await world.Auth.SignInAsync("Sitraka@exemple.mg", "motdepasse1", world.Cancel)).Value!.UserId.ShouldBe(account.Id);
    }

    [Fact]
    public async Task Refuses_wrong_passwords_unknown_identifiers_and_locked_accounts()
    {
        world.Accounts.Add(phone: "+261341234567");

        (await world.Auth.SignInAsync("0341234567", "faux", world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidCredentials);
        (await world.Auth.SignInAsync("0381234567", null, world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidCredentials);
        world.Accounts.NobodyChecks.ShouldBe(1);

        world.Accounts.LockedOut = true;
        (await world.Auth.SignInAsync("0341234567", "motdepasse1", world.Cancel)).Failure!.Error.ShouldBe(AuthError.AccountLocked);
    }

    [Theory]
    [InlineData(null, AuthError.IdentifierRequired)]
    [InlineData("12", AuthError.InvalidPhone)]
    [InlineData("a@b", AuthError.InvalidEmail)]
    public async Task Refuses_malformed_identifiers_at_sign_in(string? identifier, AuthError error) =>
        (await world.Auth.SignInAsync(identifier, "x", world.Cancel)).Failure!.Error.ShouldBe(error);

    [Fact]
    public async Task Refuses_sign_in_with_an_unconfirmed_identifier()
    {
        world.Accounts.Add(phone: "+261341234567", confirmed: false);

        (await world.Auth.SignInAsync("0341234567", "motdepasse1", world.Cancel)).Failure!.Error.ShouldBe(AuthError.AccountNotVerified);
    }

    [Fact]
    public async Task Sends_a_confirmation_again_or_to_the_email_instead()
    {
        world.Accounts.Add(phone: "+261341234567", email: "sitraka@exemple.mg", confirmed: false);

        (await world.Auth.SendConfirmationAsync("0341234567", null, world.Cancel)).Value!.Channel.ShouldBe(IdentifierKind.Phone);
        var byEmail = (await world.Auth.SendConfirmationAsync("0341234567", IdentifierKind.Email, world.Cancel)).Value!;

        byEmail.Channel.ShouldBe(IdentifierKind.Email);
        world.Sender.Sent[^1].To.ShouldBe("sitraka@exemple.mg");
        world.Wait();
        (await world.Auth.SendConfirmationAsync("sitraka@exemple.mg", IdentifierKind.Phone, world.Cancel)).Value!.Channel.ShouldBe(IdentifierKind.Phone);
    }

    [Fact]
    public async Task Refuses_to_send_a_confirmation_that_cannot_be_used()
    {
        world.Accounts.Add(phone: "+261341234567");
        world.Accounts.Add(email: "seul@exemple.mg", confirmed: false);

        (await world.Auth.SendConfirmationAsync("0341234567", null, world.Cancel)).Failure!.Error.ShouldBe(AuthError.AlreadyVerified);
        (await world.Auth.SendConfirmationAsync("0341234567", IdentifierKind.Email, world.Cancel)).Failure!.Error.ShouldBe(AuthError.AccountNotFound);
        (await world.Auth.SendConfirmationAsync("seul@exemple.mg", IdentifierKind.Phone, world.Cancel)).Failure!.Error.ShouldBe(AuthError.AccountNotFound);
        (await world.Auth.SendConfirmationAsync("0381234567", null, world.Cancel)).Failure!.Error.ShouldBe(AuthError.AccountNotFound);
        (await world.Auth.SendConfirmationAsync("", null, world.Cancel)).Failure!.Error.ShouldBe(AuthError.IdentifierRequired);
    }
}
