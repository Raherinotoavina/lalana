using Lalana.Application.Abstractions;
using Lalana.Application.Auth;
using Lalana.Application.Messaging;
using Lalana.Application.Profiles;
using Lalana.Domain.Identity;
using Lalana.Domain.Profiles;
using Microsoft.Extensions.Time.Testing;

namespace Lalana.Application.Tests.Fakes;

internal sealed class FakeCurrentUser(UserId? id) : ICurrentUser
{
    public UserId? Id { get; } = id;
}

internal sealed class InMemoryAccounts : IAccounts
{
    private readonly Dictionary<UserId, (UserAccount Account, string Password, int Failures)> accounts = [];

    public int NobodyChecks { get; private set; }

    public bool FailNextCreate { get; set; }

    public bool LockedOut { get; set; }

    public UserAccount this[UserId id] => accounts[id].Account;

    public string PasswordOf(UserId id) => accounts[id].Password;

    public Task<UserAccount?> FindByIdAsync(UserId id, CancellationToken cancellationToken) =>
        Task.FromResult(accounts.TryGetValue(id, out var entry) ? entry.Account : null);

    public Task<UserAccount?> FindAsync(LoginIdentifier identifier, CancellationToken cancellationToken) =>
        Task.FromResult(accounts.Values.Select(e => e.Account).FirstOrDefault(a =>
            identifier.Kind == IdentifierKind.Phone ? a.Phone == identifier.Phone : a.Email == identifier.Email));

    public Task<UserAccount?> CreateAsync(NewAccount account, string password, CancellationToken cancellationToken)
    {
        if (FailNextCreate)
        {
            FailNextCreate = false;
            return Task.FromResult<UserAccount?>(null);
        }

        var created = new UserAccount(UserId.From(Guid.NewGuid()), account.Phone, false, account.Email, false, account.FirstName, account.LastName);
        accounts[created.Id] = (created, password, 0);
        return Task.FromResult<UserAccount?>(created);
    }

    public Task<PasswordCheck> CheckPasswordAsync(UserId id, string password, CancellationToken cancellationToken)
    {
        if (LockedOut)
        {
            return Task.FromResult(PasswordCheck.LockedOut);
        }

        var entry = accounts[id];
        return Task.FromResult(entry.Password == password ? PasswordCheck.Valid : PasswordCheck.Wrong);
    }

    public void CheckPasswordOfNobody(string password) => NobodyChecks++;

    public Task ConfirmAsync(UserId id, LoginIdentifier identifier, CancellationToken cancellationToken)
    {
        var entry = accounts[id];
        var account = identifier.Kind == IdentifierKind.Phone
            ? entry.Account with { PhoneConfirmed = true }
            : entry.Account with { EmailConfirmed = true };
        accounts[id] = entry with { Account = account };
        return Task.CompletedTask;
    }

    public Task<bool> ReplaceAsync(UserId id, LoginIdentifier identifier, CancellationToken cancellationToken)
    {
        if (accounts.Values.Any(e => e.Account.Id != id
            && (identifier.Kind == IdentifierKind.Phone ? e.Account.Phone == identifier.Phone : e.Account.Email == identifier.Email)))
        {
            return Task.FromResult(false);
        }

        var entry = accounts[id];
        var account = identifier.Kind == IdentifierKind.Phone
            ? entry.Account with { Phone = identifier.Phone, PhoneConfirmed = true }
            : entry.Account with { Email = identifier.Email, EmailConfirmed = true };
        accounts[id] = entry with { Account = account };
        return Task.FromResult(true);
    }

    public Task SetPasswordAsync(UserId id, string password, CancellationToken cancellationToken)
    {
        accounts[id] = accounts[id] with { Password = password };
        return Task.CompletedTask;
    }

    /// <summary>Adds an account directly, for test setup.</summary>
    public UserAccount Add(string? phone = null, string? email = null, bool confirmed = true, string password = "motdepasse1")
    {
        var account = new UserAccount(
            UserId.From(Guid.NewGuid()),
            phone is null ? null : PhoneNumber.Parse(phone),
            confirmed && phone is not null,
            email is null ? null : EmailAddress.Parse(email),
            confirmed && email is not null,
            "Sitraka",
            "Rabe");
        accounts[account.Id] = (account, password, 0);
        return account;
    }

    public void Remove(UserId id) => accounts.Remove(id);
}

internal sealed class InMemoryCodes : IVerificationCodes
{
    public List<StoredCode> Codes { get; } = [];

    public Task<StoredCode?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Codes.SingleOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<(CodePurpose Purpose, DateTimeOffset SentAt)>> SentSinceAsync(string destination, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<(CodePurpose Purpose, DateTimeOffset SentAt)>>(
            [.. Codes.Where(c => c.Destination == destination && c.CreatedAt > since && c.Purpose != CodePurpose.ResetTicket).Select(c => (c.Purpose, c.CreatedAt))]);

    public Task AddAsync(StoredCode code, CancellationToken cancellationToken)
    {
        for (var i = 0; i < Codes.Count; i++)
        {
            if (Codes[i].Destination == code.Destination && Codes[i].Purpose == code.Purpose && Codes[i].ConsumedAt is null)
            {
                Codes[i] = Codes[i] with { ConsumedAt = code.CreatedAt };
            }
        }

        Codes.Add(code);
        return Task.CompletedTask;
    }

    public Task RecordFailedAttemptAsync(Guid id, CancellationToken cancellationToken)
    {
        Update(id, c => c with { FailedAttempts = c.FailedAttempts + 1 });
        return Task.CompletedTask;
    }

    public Task ConsumeAsync(Guid id, DateTimeOffset at, CancellationToken cancellationToken)
    {
        Update(id, c => c with { ConsumedAt = at });
        return Task.CompletedTask;
    }

    private void Update(Guid id, Func<StoredCode, StoredCode> change)
    {
        var index = Codes.FindIndex(c => c.Id == id);
        Codes[index] = change(Codes[index]);
    }
}

internal sealed class InMemoryRefreshTokens : IRefreshTokens
{
    public List<StoredRefreshToken> Tokens { get; } = [];

    public bool FailNextRotation { get; set; }

    public Task<StoredRefreshToken?> FindByHashAsync(string hash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.SingleOrDefault(t => t.Hash == hash));

    public Task AddAsync(StoredRefreshToken token, CancellationToken cancellationToken)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<bool> TryRotateAsync(Guid id, Guid replacedById, DateTimeOffset at, CancellationToken cancellationToken)
    {
        if (FailNextRotation)
        {
            FailNextRotation = false;
            return Task.FromResult(false);
        }

        var index = Tokens.FindIndex(t => t.Id == id);
        Tokens[index] = Tokens[index] with { ReplacedById = replacedById, RevokedAt = at };
        return Task.FromResult(true);
    }

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset at, CancellationToken cancellationToken) =>
        Revoke(t => t.FamilyId == familyId, at);

    public Task RevokeAllAsync(UserId userId, DateTimeOffset at, CancellationToken cancellationToken) =>
        Revoke(t => t.UserId == userId.Value, at);

    private Task Revoke(Func<StoredRefreshToken, bool> match, DateTimeOffset at)
    {
        for (var i = 0; i < Tokens.Count; i++)
        {
            if (match(Tokens[i]) && Tokens[i].RevokedAt is null)
            {
                Tokens[i] = Tokens[i] with { RevokedAt = at };
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryProfiles : IProfiles
{
    public Dictionary<UserId, Profile> Profiles { get; } = [];

    public Task<Profile?> FindAsync(UserId id, CancellationToken cancellationToken) =>
        Task.FromResult(Profiles.GetValueOrDefault(id));

    public Task<(Profile Profile, bool Created)> AddOrGetAsync(Profile profile, CancellationToken cancellationToken) =>
        Profiles.TryAdd(profile.Id, profile)
            ? Task.FromResult((profile, true))
            : Task.FromResult((Profiles[profile.Id], false));
}

internal sealed class PlainHasher : ICodeHasher
{
    public string Hash(string code) => "h:" + code;
}

internal sealed class FakeAccessTokens : IAccessTokenIssuer
{
    public (string Token, DateTimeOffset ExpiresAt) Issue(UserId userId, DateTimeOffset now) =>
        ($"access-{userId}", now.AddMinutes(15));
}

internal sealed class RecordingSender : ISmsSender, IEmailSender
{
    public List<(string To, string Text)> Sent { get; } = [];

    public string LastCode => System.Text.RegularExpressions.Regex.Match(Sent[^1].Text, @"\d{6}").Value;

    public Task SendAsync(PhoneNumber to, string text, CancellationToken cancellationToken)
    {
        Sent.Add((to.E164, text));
        return Task.CompletedTask;
    }

    public Task SendAsync(EmailAddress to, string subject, string text, CancellationToken cancellationToken)
    {
        Sent.Add((to.Value, text));
        return Task.CompletedTask;
    }
}

/// <summary>All the use cases over in-memory fakes and a fake clock.</summary>
internal sealed class AuthWorld
{
    public AuthWorld()
    {
        Codes = new OneTimeCodes(CodeStore, new PlainHasher(), Sender, Sender, Settings, Clock);
        Sessions = new Sessions(RefreshTokens, new FakeAccessTokens(), Settings, Clock);
        Auth = new AccountAuth(Accounts, Codes, Sessions);
        Reset = new PasswordReset(Accounts, Codes, Sessions, Clock);
        Change = new IdentifierChange(Accounts, Codes);
        Bootstrap = new BootstrapProfile(Accounts, ProfileStore, Clock);
    }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 11, 8, 0, 0, TimeSpan.Zero));

    public AuthSettings Settings { get; } = new();

    public InMemoryAccounts Accounts { get; } = new();

    public InMemoryCodes CodeStore { get; } = new();

    public InMemoryRefreshTokens RefreshTokens { get; } = new();

    public InMemoryProfiles ProfileStore { get; } = new();

    public RecordingSender Sender { get; } = new();

    public OneTimeCodes Codes { get; }

    public Sessions Sessions { get; }

    public AccountAuth Auth { get; }

    public PasswordReset Reset { get; }

    public IdentifierChange Change { get; }

    public BootstrapProfile Bootstrap { get; }

    public CancellationToken Cancel => TestContext.Current.CancellationToken;

    /// <summary>Lets the resend delay pass.</summary>
    public void Wait() => Clock.Advance(Settings.CodeResendDelay);
}
