using Lalana.Api.Hosting;
using Lalana.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddLalanaApi();

var app = builder.Build();
app.UseLalanaApi();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

await app.RunAsync();

/// <summary>Entry point, visible to the integration tests' WebApplicationFactory.</summary>
public partial class Program;
