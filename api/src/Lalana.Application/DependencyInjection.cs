using Lalana.Application.Account;
using Lalana.Application.Auth;
using Lalana.Application.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lalana.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(new AuthSettings());
        services.AddScoped<OneTimeCodes>();
        services.AddScoped<Sessions>();
        services.AddScoped<AccountAuth>();
        services.AddScoped<PasswordReset>();
        services.AddScoped<IdentifierChange>();
        services.AddScoped<BootstrapProfile>();
        services.AddScoped<GetMeHandler>();
        return services;
    }
}
