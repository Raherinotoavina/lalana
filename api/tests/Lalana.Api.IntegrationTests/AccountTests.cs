using System.Net;
using System.Net.Http.Json;
using Lalana.Api.Endpoints;
using Lalana.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class AccountTests(PostgisDatabase database)
{
    private readonly AuthApi auth = new(database.Api);

    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Bootstrap_creates_the_profile_once()
    {
        var (session, _, _) = await auth.MemberAsync();
        var client = auth.AuthorizedClient(session);

        var first = await client.PostAsJsonAsync("/profiles/bootstrap", new BootstrapProfileRequest(null, null), Cancel);
        var second = await client.PostAsJsonAsync("/profiles/bootstrap", new BootstrapProfileRequest("Autre", "Nom"), Cancel);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await auth.ReadAsync<ProfileResponse>(second, HttpStatusCode.OK);
        profile.FirstName.ShouldBe("Sitraka");
        profile.Id.ShouldBe(session.UserId);
        (await client.GetFromJsonAsync<MeResponse>("/me", AuthApi.Json, Cancel))!.HasProfile.ShouldBeTrue();
    }

    [Fact]
    public async Task Concurrent_bootstraps_create_a_single_profile()
    {
        var (session, _, _) = await auth.MemberAsync();
        var client = auth.AuthorizedClient(session);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            client.PostAsJsonAsync("/profiles/bootstrap", new BootstrapProfileRequest("Hery", "Rakoto"), Cancel)));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Bootstrap_refuses_invalid_names_and_unknown_accounts()
    {
        var (session, _, _) = await auth.MemberAsync();

        var tooLong = await auth.AuthorizedClient(session)
            .PostAsJsonAsync("/profiles/bootstrap", new BootstrapProfileRequest(new string('a', 60), "Rabe"), Cancel);
        var ghost = await database.Api.CreateClient().WithBearer(TestTokens.Create(Guid.NewGuid().ToString()))
            .PostAsJsonAsync("/profiles/bootstrap", new BootstrapProfileRequest("A", "B"), Cancel);

        (await AuthApi.ProblemCodeAsync(tooLong)).ShouldBe("name_required");
        ghost.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Adds_an_email_from_the_settings_with_a_code()
    {
        var (session, _, _) = await auth.MemberAsync();
        var client = auth.AuthorizedClient(session);
        var email = AuthApi.NewEmail();

        var sent = await auth.ReadAsync<VerificationResponse>(
            await client.PostAsJsonAsync("/me/identifiers", new ChangeIdentifierRequest(email), AuthApi.Json, Cancel), HttpStatusCode.OK);
        var confirmed = await client.PostAsJsonAsync(
            "/me/identifiers/confirm", new ConfirmCodeRequest(sent.VerificationId, auth.LastCodeTo(email)), AuthApi.Json, Cancel);

        confirmed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var me = await client.GetFromJsonAsync<MeResponse>("/me", AuthApi.Json, Cancel);
        me!.Email.ShouldBe(email);
        me.EmailConfirmed.ShouldBeTrue();
        (await auth.SignInAsync(email, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Changing_to_a_phone_used_by_another_account_is_refused()
    {
        var (_, otherPhone, _) = await auth.MemberAsync();
        var (session, _, _) = await auth.MemberAsync();
        var client = auth.AuthorizedClient(session);

        var taken = await client.PostAsJsonAsync("/me/identifiers", new ChangeIdentifierRequest(otherPhone), AuthApi.Json, Cancel);
        var wrongCode = await client.PostAsJsonAsync("/me/identifiers/confirm", new ConfirmCodeRequest(Guid.NewGuid(), "123456"), AuthApi.Json, Cancel);

        (await AuthApi.ProblemCodeAsync(taken)).ShouldBe("phone_taken");
        (await AuthApi.ProblemCodeAsync(wrongCode)).ShouldBe("code_invalid");
    }

    [Fact]
    public async Task Account_endpoints_need_a_token()
    {
        var client = database.Api.CreateClient();

        (await client.PostAsJsonAsync("/profiles/bootstrap", new BootstrapProfileRequest(null, null), Cancel)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/me/identifiers", new ChangeIdentifierRequest("x"), Cancel)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/me/identifiers/confirm", new ConfirmCodeRequest(Guid.NewGuid(), "1"), Cancel)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_development_api_shows_the_messages_it_kept()
    {
        await using var development = new LalanaApiFactory(database.ConnectionString, "Development");
        var devAuth = new AuthApi(development);
        var (local, e164) = AuthApi.NewPhone();
        await devAuth.SignUpAsync(local, null);

        var messages = await development.CreateClient().GetStringAsync("/dev/messages", Cancel);

        messages.ShouldContain(e164);
        messages.ShouldContain(devAuth.LastCodeTo(e164));
    }
}
