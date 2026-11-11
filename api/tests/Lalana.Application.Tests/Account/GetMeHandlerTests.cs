using Lalana.Application.Abstractions;
using Lalana.Application.Account;
using Lalana.Application.Auth;
using Lalana.Application.Messaging;
using Lalana.Application.Profiles;
using Lalana.Application.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Lalana.Application.Tests.Account;

public sealed class GetMeHandlerTests
{
    [Fact]
    public void AddApplication_registers_the_use_cases()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddScoped<ICurrentUser>(_ => new FakeCurrentUser(null))
            .AddSingleton<IAccounts, InMemoryAccounts>()
            .AddSingleton<IProfiles, InMemoryProfiles>()
            .AddSingleton<IVerificationCodes, InMemoryCodes>()
            .AddSingleton<IRefreshTokens, InMemoryRefreshTokens>()
            .AddSingleton<ICodeHasher, PlainHasher>()
            .AddSingleton<IAccessTokenIssuer, FakeAccessTokens>()
            .AddSingleton<RecordingSender>()
            .AddSingleton<ISmsSender>(p => p.GetRequiredService<RecordingSender>())
            .AddSingleton<IEmailSender>(p => p.GetRequiredService<RecordingSender>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<GetMeHandler>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<AccountAuth>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<PasswordReset>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IdentifierChange>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<BootstrapProfile>().ShouldNotBeNull();
    }
}
