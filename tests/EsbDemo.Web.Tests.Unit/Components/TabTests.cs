using System.Globalization;
using Bunit;
using EsbDemo.Web.Components.Pages.Tabs;
using EsbDemo.Web.Data.Crm;
using EsbDemo.Web.Data.CustomerPortal;
using EsbDemo.Web.Data.ECommerce;
using EsbDemo.Web.Data.Erp;

namespace EsbDemo.Web.Tests.Unit.Components;

public sealed class TabTests : ComponentTestBase
{
    // ── CRM ──────────────────────────────────────────────────────────────

    [Fact]
    public void CrmTab_lists_the_seeded_accounts()
    {
        var cut = Render<CrmTab>();

        Assert.Equal(5, cut.FindAll("tbody tr").Count);
        Assert.Equal("CUST0001", cut.Find("tbody tr td.mono").TextContent);
        Assert.Equal("Customer", cut.Find(".type-badge").TextContent);
    }



    [Fact]
    public void CrmTab_edit_row_saves_and_publishes_account_updated_then_closes()
    {
        var cut = Render<CrmTab>();

        cut.FindAll("tbody tr")[0].Click();
        Assert.Equal(3, cut.FindAll(".cell-input").Count);
        cut.FindAll(".cell-input")[0].Input("Acme Renamed");
        cut.FindAll(".cell-input")[1].Input("renamed@acme.com");
        cut.FindAll(".cell-input")[2].Input("555-0100");
        cut.Find(".btn-save").Click();

        Assert.Empty(cut.FindAll(".cell-input"));
        var saved = Query<CrmDbContext, CrmAccount>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0001"));
        Assert.Equal(("Acme Renamed", "renamed@acme.com", "555-0100"), (saved.Name, saved.EMailAddress1, saved.Telephone1));
        Assert.Equal("account.updated", Assert.Single(SentBy("CRM")).Topic);
    }



    [Fact]
    public void CrmTab_clicking_the_editing_row_again_or_cancel_closes_the_editor()
    {
        var cut = Render<CrmTab>();

        cut.FindAll("tbody tr")[0].Click();
        cut.FindAll("tbody tr")[0].Click();
        Assert.Empty(cut.FindAll(".cell-input"));

        cut.FindAll("tbody tr")[0].Click();
        cut.Find(".btn-cancel").Click();
        Assert.Empty(cut.FindAll(".cell-input"));
        Assert.Empty(SentBy("CRM"));
    }



    [Fact]
    public void CrmTab_status_button_toggles_the_account_and_publishes_each_change()
    {
        var cut = Render<CrmTab>();

        cut.FindAll(".status-toggle")[1].Click();
        Assert.Equal("Inactive", cut.FindAll(".status-toggle")[1].TextContent.Trim());
        cut.FindAll(".status-toggle")[1].Click();

        Assert.Equal("Active", cut.FindAll(".status-toggle")[1].TextContent.Trim());
        Assert.Equal(new[] { "account.deactivated", "account.activated" }, SentBy("CRM").Select(e => e.Topic));
        Assert.Equal(AccountStatusCode.Active, Query<CrmDbContext, AccountStatusCode>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0002").StatusCode));
    }



    [Fact]
    public void CrmTab_when_the_bus_changes_an_account_refreshes_the_list()
    {
        var cut = Render<CrmTab>();

        Monitor.Publish("account.updated", "Tester", "u-1", Fields(("CustomerId", "CUST0003"), ("Name", "Initech From Bus")));

        cut.WaitForAssertion(() => Assert.Contains("Initech From Bus", cut.Markup, StringComparison.Ordinal));
    }



    // ── ERP ──────────────────────────────────────────────────────────────

    [Fact]
    public void ErpTab_starts_on_the_list_with_the_record_count()
    {
        var cut = Render<ErpTab>();

        Assert.Contains("SELECT A CUSTOMER FROM THE LIST", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("5 RECORDS", cut.Find(".erp-trow-statusbar").TextContent, StringComparison.Ordinal);
        Assert.Equal(5, cut.FindAll(".erp-roster-row").Count);
    }



    [Fact]
    public void ErpTab_selecting_a_row_shows_the_detail_and_selecting_it_again_returns_to_the_list()
    {
        var cut = Render<ErpTab>();

        cut.FindAll(".erp-roster-row")[0].Click();
        Assert.Contains("Acme Corporation", cut.Find(".erp-terminal").TextContent, StringComparison.Ordinal);
        Assert.Contains("ACTIVE", cut.Find(".erp-terminal").TextContent, StringComparison.Ordinal);
        Assert.Contains("(open)", cut.Find(".erp-terminal").TextContent, StringComparison.Ordinal);
        Assert.Contains("erp-roster-active", cut.FindAll(".erp-roster-row")[0].ClassName, StringComparison.Ordinal);

        cut.FindAll(".erp-roster-row")[0].Click();
        Assert.Contains("SELECT A CUSTOMER FROM THE LIST", cut.Markup, StringComparison.Ordinal);

        cut.FindAll(".erp-roster-row")[1].Click();
        ByText(cut, ".erp-key", "[ESC=BACK]").Click();
        Assert.Contains("SELECT A CUSTOMER FROM THE LIST", cut.Markup, StringComparison.Ordinal);
    }



    [Fact]
    public void ErpTab_edit_saves_and_publishes_account_updated_and_cancel_discards()
    {
        var cut = Render<ErpTab>();
        cut.FindAll(".erp-roster-row")[0].Click();

        ByText(cut, ".erp-key", "[F1=EDIT]").Click();
        ByText(cut, ".erp-key", "[ESC=CANCEL]").Click();
        Assert.Empty(cut.FindAll(".erp-input"));

        ByText(cut, ".erp-key", "[F1=EDIT]").Click();
        cut.FindAll(".erp-input")[0].Input("ACME RENAMED");
        cut.FindAll(".erp-input")[1].Input("ap@acme.com");
        cut.FindAll(".erp-input")[2].Input("555-2222");
        ByText(cut, ".erp-key", "[F2=SAVE]").Click();

        var saved = Query<ErpDbContext, CustMast>(db => db.CustMast.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("ACME RENAMED", "ap@acme.com", "555-2222"), (saved.Name, saved.Email, saved.Phone));
        Assert.Equal("account.updated", Assert.Single(SentBy("ERP")).Topic);
        Assert.Empty(cut.FindAll(".erp-input"));
    }



    [Fact]
    public void ErpTab_status_key_closes_and_reopens_the_account_and_publishes_each_change()
    {
        var cut = Render<ErpTab>();
        cut.FindAll(".erp-roster-row")[0].Click();

        ByText(cut, ".erp-key", "[F3=DEACTIVATE]").Click();
        Assert.Contains("INACTIVE", cut.Find(".erp-terminal").TextContent, StringComparison.Ordinal);
        Assert.Contains("erp-roster-inactive", cut.FindAll(".erp-roster-row")[0].ClassName, StringComparison.Ordinal);
        Assert.NotNull(Query<ErpDbContext, DateTime?>(db => db.CustMast.Single(c => c.CustNum == "CUST0001").ClosedDate));

        ByText(cut, ".erp-key", "[F3=ACTIVATE]").Click();

        Assert.Null(Query<ErpDbContext, DateTime?>(db => db.CustMast.Single(c => c.CustNum == "CUST0001").ClosedDate));
        Assert.Equal(new[] { "account.deactivated", "account.activated" }, SentBy("ERP").Select(e => e.Topic));
    }



    [Fact]
    public void ErpTab_new_customer_requires_a_name_then_saves_with_defaults_and_publishes_account_created()
    {
        var cut = Render<ErpTab>();

        cut.Find(".erp-roster-new").Click();
        Assert.Contains("*** NEW CUSTOMER ***", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("CUST0006", cut.Find(".erp-terminal").TextContent, StringComparison.Ordinal);
        ByText(cut, ".erp-key", "[F2=SAVE]").Click();
        Assert.Contains("*** NEW CUSTOMER ***", cut.Markup, StringComparison.Ordinal);

        cut.FindAll(".erp-input")[0].Input("Hooli");
        cut.FindAll(".erp-input")[3].Input(" ");
        cut.FindAll(".erp-input")[4].Input(string.Empty);
        cut.FindAll(".erp-input")[5].Input("not a number");
        ByText(cut, ".erp-key", "[F2=SAVE]").Click();

        var saved = Query<ErpDbContext, CustMast>(db => db.CustMast.Single(c => c.CustNum == "CUST0006"));
        Assert.Equal(("Hooli", "GEN", (string?)null, 0m), (saved.Name, saved.CustType, saved.Terms, saved.CreditLim));
        Assert.Contains("6 RECORDS", cut.Find(".erp-trow-statusbar").TextContent, StringComparison.Ordinal);
        var sent = Assert.Single(SentBy("ERP"));
        Assert.Equal(("account.created", "CUST0006"), (sent.Topic, sent.CorrelationId));
    }



    [Fact]
    public void ErpTab_new_customer_upper_cases_type_and_terms_and_parses_the_credit_limit()
    {
        var cut = Render<ErpTab>();
        cut.Find(".erp-roster-new").Click();

        cut.FindAll(".erp-input")[0].Input("Pied Piper");
        cut.FindAll(".erp-input")[3].Input("retl");
        cut.FindAll(".erp-input")[4].Input("net60");
        cut.FindAll(".erp-input")[5].Input("2500.50");
        ByText(cut, ".erp-key", "[F2=SAVE]").Click();

        var saved = Query<ErpDbContext, CustMast>(db => db.CustMast.Single(c => c.CustNum == "CUST0006"));
        Assert.Equal(("RETL", "NET60", 2500.50m), (saved.CustType, saved.Terms, saved.CreditLim));
    }



    [Fact]
    public void ErpTab_new_customer_when_current_culture_uses_a_decimal_comma_parses_the_credit_limit_invariantly()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var cut = Render<ErpTab>();
            cut.Find(".erp-roster-new").Click();

            cut.FindAll(".erp-input")[0].Input("Hooli");
            cut.FindAll(".erp-input")[5].Input("2500.50");
            ByText(cut, ".erp-key", "[F2=SAVE]").Click();

            var saved = Query<ErpDbContext, CustMast>(db => db.CustMast.Single(c => c.CustNum == "CUST0006"));
            Assert.Equal(2500.50m, saved.CreditLim);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }



    [Fact]
    public void ErpTab_cancelling_a_new_customer_returns_to_the_list()
    {
        var cut = Render<ErpTab>();
        cut.Find(".erp-roster-new").Click();

        ByText(cut, ".erp-key", "[ESC=CANCEL]").Click();

        Assert.Contains("SELECT A CUSTOMER FROM THE LIST", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(SentBy("ERP"));
    }



    [Fact]
    public void ErpTab_clock_ticks_and_bus_changes_refresh_the_roster()
    {
        var cut = Render<ErpTab>();
        var firstClock = cut.FindAll(".erp-trow-dim")[0].TextContent;

        Monitor.Publish("account.updated", "Tester", "u-1", Fields(("CustomerId", "CUST0002"), ("Name", "Globex From Bus")));

        cut.WaitForAssertion(() => Assert.Contains("Globex From Bus", cut.Find(".erp-roster").TextContent, StringComparison.Ordinal));
        cut.WaitForAssertion
        (
            () => Assert.NotEqual(firstClock, cut.FindAll(".erp-trow-dim")[0].TextContent),
            TimeSpan.FromSeconds(5)
        );
    }



    // ── eCommerce ────────────────────────────────────────────────────────

    [Fact]
    public void ECommerceTab_edit_saves_and_publishes_and_toggle_or_cancel_closes()
    {
        var cut = Render<ECommerceTab>();
        Assert.Equal(5, cut.FindAll("tbody tr").Count);

        cut.FindAll("tbody tr")[0].Click();
        cut.FindAll("tbody tr")[0].Click();
        Assert.Empty(cut.FindAll(".cell-input"));
        cut.FindAll("tbody tr")[0].Click();
        cut.Find(".btn-cancel").Click();
        Assert.Empty(cut.FindAll(".cell-input"));

        cut.FindAll("tbody tr")[0].Click();
        cut.FindAll(".cell-input")[0].Input("Acme Shop");
        cut.FindAll(".cell-input")[1].Input("shop@acme.com");
        cut.Find(".btn-save").Click();

        var saved = Query<ECommerceDbContext, ECommerceCustomer>(db => db.Customers.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("Acme Shop", "shop@acme.com"), (saved.Name, saved.Email));
        Assert.Equal("account.updated", Assert.Single(SentBy("eCommerce")).Topic);
        Assert.Empty(cut.FindAll(".cell-input"));
    }



    [Fact]
    public void ECommerceTab_when_the_bus_deactivates_a_customer_refreshes_the_list()
    {
        var cut = Render<ECommerceTab>();

        Monitor.Publish("account.deactivated", "Tester", "d-1", Fields(("CustomerId", "CUST0004")));

        cut.WaitForAssertion(() => Assert.Contains("status-inactive", cut.FindAll("tbody tr")[3].InnerHtml, StringComparison.Ordinal));
    }



    // ── Customer Portal ──────────────────────────────────────────────────

    [Fact]
    public void CustomerPortalTab_edit_saves_and_publishes_and_toggle_or_cancel_closes()
    {
        var cut = Render<CustomerPortalTab>();
        Assert.Equal(5, cut.FindAll("tbody tr").Count);

        cut.FindAll("tbody tr")[0].Click();
        cut.FindAll("tbody tr")[0].Click();
        Assert.Empty(cut.FindAll(".cell-input"));
        cut.FindAll("tbody tr")[0].Click();
        cut.Find(".btn-cancel").Click();
        Assert.Empty(cut.FindAll(".cell-input"));

        cut.FindAll("tbody tr")[0].Click();
        cut.FindAll(".cell-input")[0].Input("Acme Portal");
        cut.FindAll(".cell-input")[1].Input("me@acme.com");
        cut.FindAll(".cell-input")[2].Input("555-7777");
        cut.Find(".btn-save").Click();

        var saved = Query<CustomerPortalDbContext, PortalCustomer>(db => db.Customers.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("Acme Portal", "me@acme.com", "555-7777"), (saved.Name, saved.Email, saved.Phone));
        Assert.Equal("account.updated", Assert.Single(SentBy("Customer Portal")).Topic);
    }



    [Fact]
    public void CustomerPortalTab_when_the_bus_creates_a_customer_adds_it_to_the_list()
    {
        var cut = Render<CustomerPortalTab>();

        Monitor.Publish("account.created", "Tester", "c-1", Fields(("CustomerId", "CUST0100"), ("Name", "Portal New")));

        cut.WaitForAssertion(() => Assert.Equal(6, cut.FindAll("tbody tr").Count));
    }



    // ── Data Warehouse ───────────────────────────────────────────────────

    [Fact]
    public void DataWarehouseTab_lists_customers_and_refreshes_when_the_bus_adds_one()
    {
        var cut = Render<DataWarehouseTab>();
        Assert.Equal(5, cut.FindAll("tbody tr").Count);

        Monitor.Publish("account.created", "Tester", "c-1", Fields(("CustomerId", "CUST0101"), ("Name", "DW New")));

        cut.WaitForAssertion(() => Assert.Equal(6, cut.FindAll("tbody tr").Count));
        var added = cut.FindAll("tbody tr")[5].TextContent;
        Assert.Contains("DW New", added, StringComparison.Ordinal);
        Assert.Contains("—", added, StringComparison.Ordinal);
    }
}
