using Lalana.Application.Auth;
using Lalana.Application.Tests.Fakes;
using Shouldly;

namespace Lalana.Application.Tests.Auth;

public sealed class IdentifierChangeTests
{
    private readonly AuthWorld world = new();

    [Fact]
    public async Task Adds_an_email_after_a_code()
    {
        var account = world.Accounts.Add(phone: "+261341234567");

        var sent = (await world.Change.RequestAsync(account.Id, "nouveau@exemple.mg", world.Cancel)).Value!;
        var updated = (await world.Change.ConfirmAsync(account.Id, sent.Id, world.Sender.LastCode, world.Cancel)).Value!;

        updated.Email!.Value.ShouldBe("nouveau@exemple.mg");
        updated.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirms_an_identifier_already_on_the_account_but_unconfirmed()
    {
        var account = world.Accounts.Add(phone: "+261341234567", email: "moi@exemple.mg", confirmed: false);

        var sent = await world.Change.RequestAsync(account.Id, "moi@exemple.mg", world.Cancel);

        sent.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Refuses_identifiers_used_elsewhere_or_already_confirmed()
    {
        var account = world.Accounts.Add(phone: "+261341234567", email: "moi@exemple.mg");
        world.Accounts.Add(phone: "+261381234567", email: "autre@exemple.mg");

        (await world.Change.RequestAsync(account.Id, "0381234567", world.Cancel)).Failure!.Error.ShouldBe(AuthError.PhoneTaken);
        (await world.Change.RequestAsync(account.Id, "autre@exemple.mg", world.Cancel)).Failure!.Error.ShouldBe(AuthError.EmailTaken);
        (await world.Change.RequestAsync(account.Id, "moi@exemple.mg", world.Cancel)).Failure!.Error.ShouldBe(AuthError.AlreadyVerified);
        (await world.Change.RequestAsync(account.Id, "1", world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidPhone);
    }

    [Fact]
    public async Task Refuses_when_someone_took_the_phone_meanwhile()
    {
        var account = world.Accounts.Add(email: "moi@exemple.mg");
        var sent = (await world.Change.RequestAsync(account.Id, "0381234567", world.Cancel)).Value!;
        world.Accounts.Add(phone: "+261381234567");

        (await world.Change.ConfirmAsync(account.Id, sent.Id, world.Sender.LastCode, world.Cancel)).Failure!.Error.ShouldBe(AuthError.PhoneTaken);
    }

    [Fact]
    public async Task Refuses_another_members_code()
    {
        var account = world.Accounts.Add(email: "moi@exemple.mg");
        var other = world.Accounts.Add(email: "autre@exemple.mg");
        var sent = (await world.Change.RequestAsync(account.Id, "0381234567", world.Cancel)).Value!;

        (await world.Change.ConfirmAsync(other.Id, sent.Id, world.Sender.LastCode, world.Cancel)).Failure!.Error.ShouldBe(AuthError.CodeInvalid);
    }
}
