using Lalana.Application.Auth;
using Lalana.Application.Tests.Fakes;
using Lalana.Domain.Identity;
using Shouldly;

namespace Lalana.Application.Tests.Auth;

public sealed class SessionsTests
{
    private readonly AuthWorld world = new();
    private readonly UserId member = UserId.From(Guid.NewGuid());

    [Fact]
    public async Task Starts_a_session_with_a_hashed_refresh_token()
    {
        var session = await world.Sessions.StartAsync(member, world.Cancel);

        session.AccessToken.ShouldBe($"access-{member}");
        var stored = world.RefreshTokens.Tokens.ShouldHaveSingleItem();
        stored.Hash.ShouldNotBe(session.RefreshToken);
        stored.ExpiresAt.ShouldBe(session.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Rotates_the_refresh_token_within_its_family()
    {
        var first = await world.Sessions.StartAsync(member, world.Cancel);

        var second = (await world.Sessions.RefreshAsync(first.RefreshToken, world.Cancel)).Value!;

        second.RefreshToken.ShouldNotBe(first.RefreshToken);
        world.RefreshTokens.Tokens.Select(t => t.FamilyId).Distinct().ShouldHaveSingleItem();
        world.RefreshTokens.Tokens[0].ReplacedById.ShouldBe(world.RefreshTokens.Tokens[1].Id);
    }

    [Fact]
    public async Task Reusing_an_old_token_ends_the_whole_family()
    {
        var first = await world.Sessions.StartAsync(member, world.Cancel);
        var second = (await world.Sessions.RefreshAsync(first.RefreshToken, world.Cancel)).Value!;

        (await world.Sessions.RefreshAsync(first.RefreshToken, world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidRefreshToken);
        (await world.Sessions.RefreshAsync(second.RefreshToken, world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidRefreshToken);
    }

    [Fact]
    public async Task Losing_a_rotation_race_counts_as_reuse()
    {
        var first = await world.Sessions.StartAsync(member, world.Cancel);
        world.RefreshTokens.FailNextRotation = true;

        (await world.Sessions.RefreshAsync(first.RefreshToken, world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidRefreshToken);
        world.RefreshTokens.Tokens.ShouldAllBe(t => t.RevokedAt != null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Refuses_missing_or_unknown_tokens(string? token) =>
        (await world.Sessions.RefreshAsync(token, world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidRefreshToken);

    [Fact]
    public async Task Refuses_an_expired_token()
    {
        var session = await world.Sessions.StartAsync(member, world.Cancel);
        world.Clock.Advance(world.Settings.RefreshTokenLifetime);

        (await world.Sessions.RefreshAsync(session.RefreshToken, world.Cancel)).Failure!.Error.ShouldBe(AuthError.InvalidRefreshToken);
    }

    [Fact]
    public async Task Ending_a_session_revokes_its_family_only()
    {
        var phone = await world.Sessions.StartAsync(member, world.Cancel);
        var tablet = await world.Sessions.StartAsync(member, world.Cancel);

        await world.Sessions.EndAsync(phone.RefreshToken, world.Cancel);
        await world.Sessions.EndAsync(null, world.Cancel);
        await world.Sessions.EndAsync("unknown", world.Cancel);

        (await world.Sessions.RefreshAsync(phone.RefreshToken, world.Cancel)).Succeeded.ShouldBeFalse();
        (await world.Sessions.RefreshAsync(tablet.RefreshToken, world.Cancel)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Ending_all_sessions_revokes_every_family()
    {
        var phone = await world.Sessions.StartAsync(member, world.Cancel);
        var tablet = await world.Sessions.StartAsync(member, world.Cancel);

        await world.Sessions.EndAllAsync(member, world.Cancel);

        (await world.Sessions.RefreshAsync(phone.RefreshToken, world.Cancel)).Succeeded.ShouldBeFalse();
        (await world.Sessions.RefreshAsync(tablet.RefreshToken, world.Cancel)).Succeeded.ShouldBeFalse();
    }
}
