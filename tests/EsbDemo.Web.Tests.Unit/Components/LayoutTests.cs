using Bunit;
using EsbDemo.Web.Components.Layout;
using EsbDemo.Web.Components.Pages;
using EsbDemo.Web.Components.Shared;
using EsbDemo.Web.Models;

namespace EsbDemo.Web.Tests.Unit.Components;

public sealed class LayoutTests : ComponentTestBase
{
    [Fact]
    public void MainLayout_renders_the_body_the_app_panel_and_the_selected_bus()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, "<p id=\"page-body\">page</p>"));

        Assert.Equal("page", cut.Find("#page-body").TextContent);
        Assert.Equal(6, cut.FindAll(".app-card").Count);
        Assert.Equal("Mock", cut.Find(".bus-badge").TextContent);
    }



    [Fact]
    public void MainLayout_when_another_bus_is_selected_switches_the_bus()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, "<p>page</p>"));

        cut.Find("#bus-select").Change("Kafka");

        Assert.Equal("Kafka", Monitor.SelectedBus);
        Assert.Equal("Kafka", cut.Find(".bus-badge").TextContent);
    }



    [Fact]
    public void AppPanel_lists_the_apps_in_display_order_with_their_actions()
    {
        var cut = Render<AppPanel>();

        Assert.Equal
        (
            new[] { "CRM", "ERP", "eCommerce", "WMS", "Customer Portal", "Data Warehouse" },
            cut.FindAll(".app-name").Select(e => e.TextContent)
        );
        Assert.Equal("Receive only", cut.Find(".receive-only-label").TextContent);
        Assert.Equal(2, cut.FindAll(".btn-action").Count(e => string.Equals(e.TextContent.Trim(), "Create Customer", StringComparison.Ordinal)));
        Assert.All(cut.FindAll(".btn-toggle"), b => Assert.Equal("Online", b.TextContent.Trim()));
    }



    [Fact]
    public void AppPanel_when_an_app_is_toggled_offline_disables_it_and_counts_its_queue()
    {
        var cut = Render<AppPanel>();

        cut.FindAll(".btn-toggle")[1].Click();
        cut.FindAll(".btn-action")[0].Click();
        cut.Find(".btn-submit").Click();

        Assert.False(Monitor.IsOnline("ERP"));
        Assert.Equal("Offline", cut.FindAll(".btn-toggle")[1].TextContent.Trim());
        Assert.Contains("offline", cut.FindAll(".app-status-dot")[1].ClassName, StringComparison.Ordinal);
        Assert.Equal("1 queued", cut.Find(".queued-badge").TextContent);
        Assert.All
        (
            cut.FindAll(".app-card")[1].QuerySelectorAll(".btn-action"),
            b => Assert.True(b.HasAttribute("disabled"))
        );

        cut.FindAll(".btn-toggle")[1].Click();

        Assert.True(Monitor.IsOnline("ERP"));
        Assert.Empty(cut.FindAll(".queued-badge"));
    }



    [Fact]
    public void AppPanel_when_a_dialog_is_sent_publishes_with_a_correlation_id_per_app_style()
    {
        var cut = Render<AppPanel>();

        cut.FindAll(".btn-action")[0].Click();
        cut.Find(".btn-submit").Click();
        var crm = Assert.Single(SentBy("CRM"));
        Assert.True(Guid.TryParse(crm.CorrelationId, out _));

        cut.FindAll(".app-card")[2].QuerySelectorAll(".btn-action")[0].Click();
        cut.Find(".btn-submit").Click();
        Assert.Matches("^[0-9]+$", Assert.Single(SentBy("eCommerce"), e => string.Equals(e.Topic, "account.create", StringComparison.Ordinal)).CorrelationId);

        ByText(cut, ".btn-action", "Customer Created").Click();
        cut.Find(".dialog-input").Input("CUST4321");
        cut.Find(".btn-submit").Click();
        Assert.Equal("CUST4321", Assert.Single(SentBy("ERP"), e => e.Summary.Contains("CUST4321", StringComparison.Ordinal)).CorrelationId);

        ByText(cut, ".btn-action", "Customer Created").Click();
        cut.Find(".dialog-input").Input(string.Empty);
        cut.Find(".btn-submit").Click();
        Assert.True(Guid.TryParse(SentBy("ERP")[^1].CorrelationId, out _));

        ByText(cut, ".btn-action", "Order Shipped").Click();
        cut.Find(".btn-submit").Click();
        Assert.True(Guid.TryParse(Assert.Single(SentBy("WMS")).CorrelationId, out _));
    }



    [Fact]
    public async Task ActionDialog_when_opened_shows_the_topic_with_resolved_defaults_and_sends_edits()
    {
        ActionDialogResult? sent = null;
        var cut = Render<ActionDialog>(p => p.Add(d => d.OnSubmit, r => sent = r));

        await cut.InvokeAsync(() => cut.Instance.Open("account.create", "CRM"));

        Assert.Equal("Create Customer", cut.Find(".dialog-title").TextContent);
        Assert.Equal("CRM → account.create", cut.Find(".dialog-subtitle").TextContent);
        var inputs = cut.FindAll(".dialog-input");
        Assert.Equal(3, inputs.Count);
        Assert.Matches("^Acme Corp [0-9]{2}$", inputs[0].GetAttribute("value"));
        Assert.False(cut.Find(".btn-submit").HasAttribute("disabled"));

        inputs[0].Input("Typed Name");
        cut.Find(".btn-submit").Click();

        Assert.NotNull(sent);
        Assert.Equal(("account.create", "CRM", "Typed Name"), (sent.Topic, sent.SourceApp, sent.Fields["Name"]));
        Assert.Empty(cut.FindAll(".dialog-panel"));
    }



    [Fact]
    public async Task ActionDialog_closes_on_cancel_or_on_a_backdrop_click()
    {
        var cut = Render<ActionDialog>();
        await cut.InvokeAsync(() => cut.Instance.Open("account.create", "CRM"));

        cut.Find(".btn-cancel").Click();
        Assert.Empty(cut.FindAll(".dialog-panel"));

        await cut.InvokeAsync(() => cut.Instance.Open("account.create", "CRM"));
        cut.Find(".dialog-backdrop").Click();
        Assert.Empty(cut.FindAll(".dialog-panel"));
    }



    [Fact]
    public async Task ActionDialog_when_the_topic_is_unknown_shows_no_fields()
    {
        var cut = Render<ActionDialog>();

        await cut.InvokeAsync(() => cut.Instance.Open("custom.topic", "Tester"));

        Assert.Equal("custom.topic", cut.Find(".dialog-title").TextContent);
        Assert.Empty(cut.FindAll(".dialog-field"));
    }



    [Fact]
    public async Task ActionDialog_when_no_customers_exist_says_so_and_disables_send()
    {
        var cut = Render<ActionDialog>();

        await cut.InvokeAsync(() => cut.Instance.Open("account.updated", "CRM"));

        Assert.Contains("No customers created yet", cut.Find(".dialog-no-customers").TextContent, StringComparison.Ordinal);
        Assert.True(cut.Find(".btn-submit").HasAttribute("disabled"));
    }



    [Fact]
    public async Task ActionDialog_customer_picker_lists_customers_and_pending_requests_and_copies_the_choice()
    {
        Monitor.Publish("account.created", "ERP", "CUST0099", Fields(("CustomerId", "CUST0099"), ("Name", "New Co"), ("Email", "n@new.co")));
        Monitor.Publish("account.create", "CRM", "corr-p", Fields(("Name", "Waiting Co")));
        ActionDialogResult? sent = null;
        var cut = Render<ActionDialog>(p => p.Add(d => d.OnSubmit, r => sent = r));
        await cut.InvokeAsync(() => cut.Instance.Open("account.updated", "CRM"));

        var options = cut.FindAll(".dialog-select option").Select(o => o.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "— select a customer —", "CUST0099 — New Co", "⏳ Waiting Co [Pending]" }, options);
        Assert.Equal("Pending", cut.Find("optgroup").GetAttribute("label"));

        cut.Find(".dialog-select").Change("CUST0099");
        Assert.False(cut.Find(".btn-submit").HasAttribute("disabled"));

        cut.Find(".dialog-select").Change(string.Empty);
        Assert.True(cut.Find(".btn-submit").HasAttribute("disabled"));

        cut.Find(".dialog-select").Change("CUST7777");
        cut.Find(".dialog-select").Change("CUST0099");
        cut.Find(".btn-submit").Click();

        Assert.Equal("CUST0099", sent!.Fields["CustomerId"]);
        Assert.Equal("New Co", sent.Fields["Name"]);
        Assert.Equal("n@new.co", sent.Fields["Email"]);
    }
}
