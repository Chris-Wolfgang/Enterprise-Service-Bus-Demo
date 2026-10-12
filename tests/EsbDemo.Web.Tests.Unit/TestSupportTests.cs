using Xunit.Sdk;

namespace EsbDemo.Web.Tests.Unit;

public sealed class TestSupportTests
{
    [Fact]
    public async Task WaitUntilAsync_when_the_condition_becomes_true_later_returns()
    {
        var calls = 0;

        await TestConfig.WaitUntilAsync(() => ++calls >= 3);

        Assert.Equal(3, calls);
    }



    [Fact]
    public async Task WaitUntilAsync_when_the_condition_never_holds_fails_after_the_timeout()
    {
        await Assert.ThrowsAsync<TrueException>
        (
            () => TestConfig.WaitUntilAsync(() => false, TimeSpan.FromMilliseconds(30))
        );
    }
}
