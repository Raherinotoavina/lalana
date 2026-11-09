using Lalana.Domain.Identity;
using Shouldly;

namespace Lalana.Domain.Tests.Identity;

public sealed class UserIdTests
{
    [Fact]
    public void From_keeps_a_non_empty_guid()
    {
        var guid = Guid.NewGuid();

        var id = UserId.From(guid);

        id.Value.ShouldBe(guid);
        id.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void From_rejects_the_empty_guid() =>
        Should.Throw<ArgumentException>(() => UserId.From(Guid.Empty));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void TryParse_rejects_missing_invalid_or_empty_ids(string? text)
    {
        UserId.TryParse(text, out var id).ShouldBeFalse();
        id.ShouldBe(default);
    }

    [Fact]
    public void TryParse_accepts_a_guid()
    {
        var guid = Guid.NewGuid();

        UserId.TryParse(guid.ToString(), out var id).ShouldBeTrue();
        id.ShouldBe(UserId.From(guid));
    }
}
