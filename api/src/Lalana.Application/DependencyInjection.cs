using Lalana.Application.Account;
using Microsoft.Extensions.DependencyInjection;

namespace Lalana.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetMeHandler>();
        return services;
    }
}
