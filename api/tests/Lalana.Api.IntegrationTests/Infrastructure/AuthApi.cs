using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lalana.Api.Endpoints;
using Lalana.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Lalana.Api.IntegrationTests.Infrastructure;

/// <summary>Drives the auth endpoints like the app does, reading codes from the development sender.</summary>
internal sealed partial class AuthApi(LalanaApiFactory factory)
{
    public static readonly JsonSerializerOptions Json = CreateJson();

    public HttpClient Client { get; } = factory.CreateClient();

    private DevelopmentMessages Messages => factory.Services.GetRequiredService<DevelopmentMessages>();

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    /// <summary>A phone nobody else in the test run uses, in local form, and its E.164 form.</summary>
    public static (string Local, string E164) NewPhone()
    {
        var digits = Random.Shared.Next(0, 10_000_000).ToString("D7", System.Globalization.CultureInfo.InvariantCulture);
        return ($"034 {digits[..2]} {digits[2..5]} {digits[5..]}", "+26134" + digits);
    }

    public static string NewEmail() => $"membre-{Guid.NewGuid():N}@exemple.mg";

    public string LastCodeTo(string destination) =>
        CodePattern().Match(Messages.LastTo(destination)?.Text ?? string.Empty).Value;

    public Task<HttpResponseMessage> PostAsync(string path, object body) =>
        Client.PostAsJsonAsync(path, body, Json, Cancel);

    public async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync(Cancel));
        return (await response.Content.ReadFromJsonAsync<T>(Json, Cancel))!;
    }

    public static async Task<string> ProblemCodeAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel));
        return document.RootElement.GetProperty("code").GetString()!;
    }

    public async Task<VerificationResponse> SignUpAsync(string? phone, string? email, string password = "sitraka2026") =>
        await ReadAsync<VerificationResponse>(
            await PostAsync("/auth/sign-up", new SignUpRequest("Sitraka", "Rabe", phone, email, password)),
            HttpStatusCode.Created);

    public async Task<SessionResponse> ConfirmAsync(VerificationResponse verification, string destination) =>
        await ReadAsync<SessionResponse>(
            await PostAsync("/auth/confirm", new ConfirmCodeRequest(verification.VerificationId, LastCodeTo(destination))),
            HttpStatusCode.OK);

    /// <summary>Signs up with a fresh phone, confirms it, and returns the session.</summary>
    public async Task<(SessionResponse Session, string Phone, string E164)> MemberAsync(string? email = null, string password = "sitraka2026")
    {
        var (local, e164) = NewPhone();
        var verification = await SignUpAsync(local, email, password);
        return (await ConfirmAsync(verification, e164), local, e164);
    }

    public Task<HttpResponseMessage> SignInAsync(string identifier, string password) =>
        PostAsync("/auth/sign-in", new SignInRequest(identifier, password));

    public HttpClient AuthorizedClient(SessionResponse session) => factory.CreateClient().WithBearer(session.AccessToken);

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    [GeneratedRegex(@"\d{6}")]
    private static partial Regex CodePattern();
}
