using Lalana.Application.Account;
using Lalana.Application.Auth;
using Lalana.Application.Tests.Fakes;
using Lalana.Domain.Identity;
using Shouldly;

namespace Lalana.Application.Tests.Auth;

public sealed class ProfileTests
{
    private readonly AuthWorld world = new();

    [Fact]
    public async Task Bootstraps_once_from_the_sign_up_names()
    {
        var account = world.Accounts.Add(phone: "+261341234567");

        var (first, created) = (await world.Bootstrap.HandleAsync(account.Id, null, " ", world.Cancel)).Value;
        var (again, createdAgain) = (await world.Bootstrap.HandleAsync(account.Id, "Autre", "Nom", world.Cancel)).Value;

        created.ShouldBeTrue();
        createdAgain.ShouldBeFalse();
        first.FirstName.ShouldBe("Sitraka");
        again.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task Uses_the_submitted_names_when_given()
    {
        var account = world.Accounts.Add(phone: "+261341234567");

        var (profile, _) = (await world.Bootstrap.HandleAsync(account.Id, "  Hery  Nirina ", "Rakoto", world.Cancel)).Value;

        profile.FirstName.ShouldBe("Hery Nirina");
        profile.LastName.ShouldBe("Rakoto");
    }

    [Fact]
    public async Task Refuses_names_that_are_too_long_or_a_missing_account()
    {
        var account = world.Accounts.Add(phone: "+261341234567");

        (await world.Bootstrap.HandleAsync(account.Id, new string('a', 51), "Rabe", world.Cancel)).Failure!.Error.ShouldBe(AuthError.NameRequired);
        (await world.Bootstrap.HandleAsync(UserId.From(Guid.NewGuid()), "A", "B", world.Cancel)).Failure!.Error.ShouldBe(AuthError.AccountNotFound);
    }

    [Fact]
    public async Task Me_shows_the_account_then_the_profile()
    {
        var account = world.Accounts.Add(phone: "+261341234567", email: "moi@exemple.mg");
        var handler = new GetMeHandler(new FakeCurrentUser(account.Id), world.Accounts, world.ProfileStore);

        var before = (await handler.HandleAsync(world.Cancel))!;
        await world.Bootstrap.HandleAsync(account.Id, "Hery", "Rakoto", world.Cancel);
        var after = (await handler.HandleAsync(world.Cancel))!;

        before.ShouldBe(new MeResult(account.Id.Value, "Sitraka", "Rabe", "+261341234567", true, "moi@exemple.mg", true, false));
        after.FirstName.ShouldBe("Hery");
        after.HasProfile.ShouldBeTrue();
    }

    [Fact]
    public async Task Me_is_null_without_identity_or_account()
    {
        (await new GetMeHandler(new FakeCurrentUser(null), world.Accounts, world.ProfileStore).HandleAsync(world.Cancel)).ShouldBeNull();
        (await new GetMeHandler(new FakeCurrentUser(UserId.From(Guid.NewGuid())), world.Accounts, world.ProfileStore).HandleAsync(world.Cancel)).ShouldBeNull();
    }
}
