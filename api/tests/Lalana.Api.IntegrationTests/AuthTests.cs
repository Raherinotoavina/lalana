using System.Net;
using System.Net.Http.Json;
using Lalana.Api.Endpoints;
using Lalana.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Lalana.Api.IntegrationTests;

public sealed class AuthTests(PostgisDatabase database)
{
    private readonly AuthApi auth = new(database.Api);

    private CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Signs_up_with_a_phone_only_confirms_by_sms_and_signs_in()
    {
        var (local, e164) = AuthApi.NewPhone();

        var verification = await auth.SignUpAsync(local, null);
        verification.Channel.ShouldBe(CodeChannel.Sms);
        verification.MaskedDestination.ShouldBe($"+261 34 ** *** {e164[^2..]}");
        var session = await auth.ConfirmAsync(verification, e164);

        var me = await auth.AuthorizedClient(session).GetFromJsonAsync<MeResponse>("/me", AuthApi.Json, Cancel);
        me!.Phone.ShouldBe(e164);
        me.PhoneConfirmed.ShouldBeTrue();
        (await auth.SignInAsync(local, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Signs_up_with_an_email_only_confirms_by_email()
    {
        var email = AuthApi.NewEmail();

        var verification = await auth.SignUpAsync(null, email.ToUpperInvariant());
        verification.Channel.ShouldBe(CodeChannel.Email);
        await auth.ConfirmAsync(verification, email);

        (await auth.SignInAsync(email, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task With_both_signs_in_by_phone_and_by_email_once_the_email_is_confirmed()
    {
        var email = AuthApi.NewEmail();
        var (_, local, _) = await auth.MemberAsync(email);

        (await auth.SignInAsync(local, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var unconfirmed = await auth.SignInAsync(email, "sitraka2026");
        unconfirmed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await AuthApi.ProblemCodeAsync(unconfirmed)).ShouldBe("account_not_verified");

        var code = await auth.ReadAsync<VerificationResponse>(
            await auth.PostAsync("/auth/codes", new SendCodeRequest(email, null)), HttpStatusCode.OK);
        await auth.ConfirmAsync(code, email);
        (await auth.SignInAsync(email, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sends_the_confirmation_to_the_email_when_sms_does_not_arrive()
    {
        var email = AuthApi.NewEmail();
        var (local, _) = AuthApi.NewPhone();
        await auth.SignUpAsync(local, email);

        var byEmail = await auth.ReadAsync<VerificationResponse>(
            await auth.PostAsync("/auth/codes", new SendCodeRequest(local, CodeChannel.Email)), HttpStatusCode.OK);

        byEmail.Channel.ShouldBe(CodeChannel.Email);
        await auth.ConfirmAsync(byEmail, email);
        (await auth.SignInAsync(email, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await auth.SignInAsync(local, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(null, null, "sitraka2026", "identifier_required")]
    [InlineData("034 12 34", null, "sitraka2026", "invalid_phone")]
    [InlineData(null, "pas-un-email", "sitraka2026", "invalid_email")]
    [InlineData("034 99 999 99", null, "rabesitraka", "weak_password")]
    public async Task Refuses_invalid_sign_ups_with_a_clear_code(string? phone, string? email, string password, string code)
    {
        var response = await auth.PostAsync("/auth/sign-up", new SignUpRequest("Sitraka", "Rabe", phone, email, password));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ProblemCodeAsync(response)).ShouldBe(code);
    }

    [Fact]
    public async Task Refuses_a_phone_or_email_already_used()
    {
        var email = AuthApi.NewEmail();
        var (_, local, _) = await auth.MemberAsync(email);

        var phoneTaken = await auth.PostAsync("/auth/sign-up", new SignUpRequest("A", "B", local, null, "sitraka2026"));
        var emailTaken = await auth.PostAsync("/auth/sign-up", new SignUpRequest("A", "B", null, email, "sitraka2026"));

        phoneTaken.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await AuthApi.ProblemCodeAsync(phoneTaken)).ShouldBe("phone_taken");
        (await AuthApi.ProblemCodeAsync(emailTaken)).ShouldBe("email_taken");
    }

    [Fact]
    public async Task Wrong_codes_count_and_lock_the_code()
    {
        var (local, e164) = AuthApi.NewPhone();
        var verification = await auth.SignUpAsync(local, null);
        var codes = new List<string>();

        for (var i = 0; i < 5; i++)
        {
            codes.Add(await AuthApi.ProblemCodeAsync(
                await auth.PostAsync("/auth/confirm", new ConfirmCodeRequest(verification.VerificationId, "000000x"))));
        }

        codes.ShouldBe(["code_invalid", "code_invalid", "code_invalid", "code_invalid", "code_attempts_exceeded"]);
        var right = await auth.PostAsync("/auth/confirm", new ConfirmCodeRequest(verification.VerificationId, auth.LastCodeTo(e164)));
        (await AuthApi.ProblemCodeAsync(right)).ShouldBe("code_attempts_exceeded");
    }

    [Fact]
    public async Task Repeated_code_requests_for_the_same_number_are_rate_limited()
    {
        var (local, _) = AuthApi.NewPhone();
        await auth.SignUpAsync(local, null);

        var again = await auth.PostAsync("/auth/codes", new SendCodeRequest(local, null));

        again.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        again.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ShouldBeInRange(1, 60);
        (await AuthApi.ProblemCodeAsync(again)).ShouldBe("code_rate_limited");
    }

    [Fact]
    public async Task Refuses_codes_for_unknown_or_confirmed_identifiers()
    {
        var (_, local, _) = await auth.MemberAsync();

        var unknown = await auth.PostAsync("/auth/codes", new SendCodeRequest(AuthApi.NewEmail(), null));
        var confirmed = await auth.PostAsync("/auth/codes", new SendCodeRequest(local, CodeChannel.Sms));

        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await AuthApi.ProblemCodeAsync(confirmed)).ShouldBe("already_verified");
    }

    [Fact]
    public async Task Wrong_password_unknown_account_and_lockout_after_five_failures()
    {
        var (_, local, _) = await auth.MemberAsync();

        var unknown = await auth.SignInAsync(AuthApi.NewPhone().Local, "sitraka2026");
        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ProblemCodeAsync(unknown)).ShouldBe("invalid_credentials");

        var answers = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            answers.Add(await AuthApi.ProblemCodeAsync(await auth.SignInAsync(local, "mauvais2026")));
        }

        answers.ShouldBe(["invalid_credentials", "invalid_credentials", "invalid_credentials", "invalid_credentials", "account_locked"]);
        var locked = await auth.SignInAsync(local, "sitraka2026");
        locked.StatusCode.ShouldBe((HttpStatusCode)423);
    }

    [Fact]
    public async Task Refresh_rotates_and_reusing_an_old_token_ends_the_session()
    {
        var (session, _, _) = await auth.MemberAsync();

        var rotated = await auth.ReadAsync<SessionResponse>(
            await auth.PostAsync("/auth/refresh", new RefreshRequest(session.RefreshToken)), HttpStatusCode.OK);
        var reuse = await auth.PostAsync("/auth/refresh", new RefreshRequest(session.RefreshToken));
        var afterReuse = await auth.PostAsync("/auth/refresh", new RefreshRequest(rotated.RefreshToken));

        rotated.RefreshToken.ShouldNotBe(session.RefreshToken);
        (await auth.AuthorizedClient(rotated).GetAsync("/me", Cancel)).StatusCode.ShouldBe(HttpStatusCode.OK);
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ProblemCodeAsync(reuse)).ShouldBe("invalid_refresh_token");
        afterReuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sign_out_revokes_the_refresh_token()
    {
        var (session, _, _) = await auth.MemberAsync();

        (await auth.PostAsync("/auth/sign-out", new RefreshRequest(session.RefreshToken))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await auth.PostAsync("/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resets_the_password_from_a_phone_and_ends_old_sessions()
    {
        var (session, local, e164) = await auth.MemberAsync();

        var sent = await auth.ReadAsync<VerificationResponse>(
            await auth.PostAsync("/auth/password/forgot", new ForgotPasswordRequest(local)), HttpStatusCode.OK);
        var ticket = await auth.ReadAsync<ResetTicketResponse>(
            await auth.PostAsync("/auth/password/verify", new ConfirmCodeRequest(sent.VerificationId, auth.LastCodeTo(e164))), HttpStatusCode.OK);
        var signedIn = await auth.ReadAsync<SessionResponse>(
            await auth.PostAsync("/auth/password/reset", new ResetPasswordRequest(ticket.ResetTicket, "nouveau2026")), HttpStatusCode.OK);

        signedIn.UserId.ShouldBe(session.UserId);
        (await auth.SignInAsync(local, "sitraka2026")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await auth.SignInAsync(local, "nouveau2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await auth.PostAsync("/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var reused = await auth.PostAsync("/auth/password/reset", new ResetPasswordRequest(ticket.ResetTicket, "encore2026"));
        (await AuthApi.ProblemCodeAsync(reused)).ShouldBe("invalid_reset_ticket");
    }

    [Fact]
    public async Task Resets_the_password_from_an_email()
    {
        var email = AuthApi.NewEmail();
        var verification = await auth.SignUpAsync(null, email);
        await auth.ConfirmAsync(verification, email);

        var sent = await auth.ReadAsync<VerificationResponse>(
            await auth.PostAsync("/auth/password/forgot", new ForgotPasswordRequest(email)), HttpStatusCode.OK);
        var ticket = await auth.ReadAsync<ResetTicketResponse>(
            await auth.PostAsync("/auth/password/verify", new ConfirmCodeRequest(sent.VerificationId, auth.LastCodeTo(email))), HttpStatusCode.OK);
        await auth.ReadAsync<SessionResponse>(
            await auth.PostAsync("/auth/password/reset", new ResetPasswordRequest(ticket.ResetTicket, "nouveau2026")), HttpStatusCode.OK);

        (await auth.SignInAsync(email, "nouveau2026")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_reset_screen_gets_the_same_answer_whether_or_not_the_account_exists()
    {
        var (_, local, _) = await auth.MemberAsync();
        var (unknownLocal, _) = AuthApi.NewPhone();

        var known = await auth.PostAsync("/auth/password/forgot", new ForgotPasswordRequest(local));
        var unknown = await auth.PostAsync("/auth/password/forgot", new ForgotPasswordRequest(unknownLocal));

        known.StatusCode.ShouldBe(HttpStatusCode.OK);
        unknown.StatusCode.ShouldBe(HttpStatusCode.OK);
        var knownBody = await auth.ReadAsync<VerificationResponse>(known, HttpStatusCode.OK);
        var unknownBody = await auth.ReadAsync<VerificationResponse>(unknown, HttpStatusCode.OK);
        knownBody.Channel.ShouldBe(unknownBody.Channel);
        (knownBody.ExpiresAt - knownBody.ResendAvailableAt).ShouldBe(unknownBody.ExpiresAt - unknownBody.ResendAvailableAt);
    }

    [Fact]
    public async Task Malformed_requests_get_problem_details()
    {
        var badPhone = await auth.PostAsync("/auth/sign-in", new SignInRequest("12", "x"));
        var badReset = await auth.PostAsync("/auth/password/forgot", new ForgotPasswordRequest(null));
        var badCode = await auth.PostAsync("/auth/password/verify", new ConfirmCodeRequest(Guid.NewGuid(), "123456"));

        (await AuthApi.ProblemCodeAsync(badPhone)).ShouldBe("invalid_phone");
        (await AuthApi.ProblemCodeAsync(badReset)).ShouldBe("identifier_required");
        (await AuthApi.ProblemCodeAsync(badCode)).ShouldBe("code_invalid");
    }

    [Fact]
    public async Task Auth_calls_are_limited_per_ip_and_per_device()
    {
        await using var limited = new LalanaApiFactory(database.ConnectionString, authPermitsPerMinute: 2);
        var client = limited.CreateClient();
        var request = new ForgotPasswordRequest(AuthApi.NewPhone().Local);

        await client.PostAsJsonAsync("/auth/password/forgot", request, Cancel);
        await client.PostAsJsonAsync("/auth/password/forgot", request, Cancel);
        var third = await client.PostAsJsonAsync("/auth/password/forgot", request, Cancel);
        var otherPaths = await client.GetAsync("/health", Cancel);

        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await AuthApi.ProblemCodeAsync(third)).ShouldBe("too_many_requests");
        otherPaths.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
