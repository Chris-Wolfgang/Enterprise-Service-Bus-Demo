using EsbDemo.Web.Models;

namespace EsbDemo.Web.Tests.Unit.Models;

public sealed class MonitorEntryTests
{
    [Theory]
    [InlineData(MonitorDirection.Send, "→ SEND")]
    [InlineData(MonitorDirection.Receive, "← RECV")]
    [InlineData(MonitorDirection.Timeout, "⚠ TIMEOUT")]
    public void DirectionLabel_when_Direction_is_set_returns_the_monitor_label(MonitorDirection direction, string expected)
    {
        var sut = new MonitorEntry { Direction = direction };

        Assert.Equal(expected, sut.DirectionLabel);
    }



    [Fact]
    public void Direction_when_not_set_is_Send()
    {
        var sut = new MonitorEntry();

        Assert.Equal(MonitorDirection.Send, sut.Direction);
    }
}
