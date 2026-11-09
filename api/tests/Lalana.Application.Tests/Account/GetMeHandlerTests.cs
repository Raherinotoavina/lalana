using Lalana.Application.Abstractions;
using Lalana.Application.Account;
using Lalana.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Lalana.Application.Tests.Account;

public sealed class GetMeHandlerTests
{
    private sealed class FakeCurrentUser(UserId? id) : ICurrentUser
    {
        public UserId? Id { get; } = id;
    }

    [Fact]
    public void Returns_the_callers_id()
    {
        var id = UserId.From(Guid.NewGuid());

        var result = new GetMeHandler(new FakeCurrentUser(id)).Handle();

        result.ShouldBe(new MeResult(id.Value));
    }

    [Fact]
    public void Returns_null_without_an_identity() =>
        new GetMeHandler(new FakeCurrentUser(null)).Handle().ShouldBeNull();

    [Fact]
    public void AddApplication_registers_the_handler()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddScoped<ICurrentUser>(_ => new FakeCurrentUser(null));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<GetMeHandler>().ShouldNotBeNull();
    }
}
