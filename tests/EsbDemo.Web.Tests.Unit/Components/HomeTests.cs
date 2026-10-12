using Bunit;
using EsbDemo.Web.Components.Pages;

namespace EsbDemo.Web.Tests.Unit.Components;

public sealed class HomeTests : ComponentTestBase
{
    [Fact]
    public void Home_when_the_bus_is_empty_shows_the_empty_monitor()
    {
        var cut = Render<Home>();

        Assert.Contains("No messages yet", cut.Find(".monitor-empty").TextContent, StringComparison.Ordinal);
        Assert.Contains("active", ByText(cut, ".tab-btn", "Monitor").ClassName, StringComparison.Ordinal);
    }



    [Fact]
    public void Home_when_messages_are_published_lists_them_and_scrolls_the_monitor()
    {
        var cut = Render<Home>();

        Monitor.Publish("account.deactivated", "CRM", null, Fields(("CustomerId", "CUST0001"), ("Name", "Acme")));

        cut.WaitForAssertion(() => Assert.Equal(6, cut.FindAll("tbody tr").Count));
        var send = cut.FindAll("tbody tr")[0];
        Assert.Equal("row-send", send.ClassName);
        Assert.Equal("account.deactivated", send.QuerySelector(".topic-badge")!.TextContent);
        Assert.Contains("topic-red-dark", send.QuerySelector(".topic-badge")!.ClassName, StringComparison.Ordinal);
        Assert.Equal("—", send.QuerySelector(".col-corr")!.TextContent);
        Assert.Equal("row-recv", cut.FindAll("tbody tr")[1].ClassName);
        Assert.NotEmpty(JSInterop.Invocations["scrollMonitorToBottom"]);
    }



    [Fact]
    public void Home_styles_a_delivery_that_waited_in_the_queue_as_queued()
    {
        Monitor.SetOnline("ERP", online: false);
        Monitor.Publish("account.updated", "CRM", "u-1", Fields(("CustomerId", "CUST0001")));
        Monitor.SetOnline("ERP", online: true);

        var cut = Render<Home>();

        var rows = cut.FindAll("tbody tr");
        var queued = Assert.Single(rows, r => r.ClassList.Contains("queued"));
        Assert.Equal("ERP", queued.QuerySelector(".col-app")!.TextContent);
        Assert.Equal("row-recv queued", queued.ClassName);
    }



    [Theory]
    [InlineData("CRM", ".crm-tab")]
    [InlineData("ERP", ".erp-container")]
    [InlineData("eCommerce", ".ecommerce-tab")]
    [InlineData("WMS", ".wms-tab")]
    [InlineData("Customer Portal", ".portal-tab")]
    [InlineData("Data Warehouse", ".dw-tab")]
    public void Home_when_a_tab_is_clicked_shows_that_system(string tab, string selector)
    {
        var cut = Render<Home>();

        ByText(cut, ".tab-btn", tab).Click();

        Assert.NotNull(cut.Find(selector));
        Assert.Contains("active", ByText(cut, ".tab-btn", tab).ClassName, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".monitor-container"));
    }



    [Fact]
    public void Home_when_the_bus_changes_on_another_tab_rerenders_without_scrolling()
    {
        var cut = Render<Home>();
        ByText(cut, ".tab-btn", "WMS").Click();

        Monitor.Publish("account.updated", "CRM", "u-1", Fields(("CustomerId", "CUST0001")));
        ByText(cut, ".tab-btn", "Monitor").Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")));
        Assert.Equal("u-1", cut.FindAll(".col-corr")[0].TextContent);
        Assert.Empty(JSInterop.Invocations["scrollMonitorToBottom"]);
    }
}
