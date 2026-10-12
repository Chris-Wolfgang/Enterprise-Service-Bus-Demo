using EsbDemo.Web.Connectors;
using EsbDemo.Web.Data;
using EsbDemo.Web.Data.Crm;
using EsbDemo.Web.Data.CustomerPortal;
using EsbDemo.Web.Data.DataWarehouse;
using EsbDemo.Web.Data.ECommerce;
using EsbDemo.Web.Data.Erp;
using EsbDemo.Web.Models;

namespace EsbDemo.Web.Tests.Unit.Connectors;

public sealed class ConnectorTests
{
    private static Dictionary<string, string> Fields(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);



    private static MonitorEntry SentBy(ConnectorHarness h, string app) =>
        Assert.Single
        (
            h.Monitor.Entries,
            e => string.Equals(e.AppName, app, StringComparison.Ordinal) && string.Equals(e.Direction, "→ SEND", StringComparison.Ordinal)
        );



    [Fact]
    public void Constructors_when_monitor_is_null_throw_ArgumentNullException()
    {
        var scopes = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        Assert.Equal("monitor", Assert.Throws<ArgumentNullException>(() => new CrmConnector(null!, scopes)).ParamName);
        Assert.Equal("monitor", Assert.Throws<ArgumentNullException>(() => new ErpConnector(null!, scopes)).ParamName);
        Assert.Equal("monitor", Assert.Throws<ArgumentNullException>(() => new ECommerceConnector(null!, scopes)).ParamName);
        Assert.Equal("monitor", Assert.Throws<ArgumentNullException>(() => new CustomerPortalConnector(null!, scopes)).ParamName);
        Assert.Equal("monitor", Assert.Throws<ArgumentNullException>(() => new DataWarehouseConnector(null!, scopes)).ParamName);
    }



    [Fact]
    public async Task Publish_methods_when_the_record_is_null_throw_ArgumentNullException()
    {
        using var h = await ConnectorHarness.CreateAsync();

        Assert.Throws<ArgumentNullException>(() => h.Crm.PublishAccountUpdated(null!));
        Assert.Throws<ArgumentNullException>(() => h.Crm.PublishStatusChanged(null!));
        Assert.Throws<ArgumentNullException>(() => h.Erp.PublishAccountUpdated(null!));
        Assert.Throws<ArgumentNullException>(() => h.Erp.PublishStatusChanged(null!, wasActive: true));
        Assert.Throws<ArgumentNullException>(() => h.Erp.PublishNew(null!));
        Assert.Throws<ArgumentNullException>(() => h.ECommerce.PublishAccountUpdated(null!));
        Assert.Throws<ArgumentNullException>(() => h.Portal.PublishAccountUpdated(null!));
    }



    [Fact]
    public async Task CrmConnector_PublishAccountUpdated_maps_CRM_fields_to_the_canonical_names()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var acct = new CrmAccount { AccountId = WellKnown.Accounts.AcmeId, AccountNumber = "CUST0001", Name = "Acme", EMailAddress1 = "a@acme.com", Telephone1 = "555-1" };

        h.Crm.PublishAccountUpdated(acct);

        var sent = SentBy(h, "CRM");
        Assert.Equal("account.updated", sent.Topic);
        Assert.Equal(WellKnown.Accounts.AcmeId.ToString(), sent.CorrelationId);
        Assert.Equal("CustomerId: CUST0001, Name: Acme, Email: a@acme.com, Phone: 555-1", sent.Summary);
    }



    [Fact]
    public async Task CrmConnector_PublishAccountUpdated_when_optional_fields_are_missing_sends_the_account_id_and_blanks()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var id = Guid.NewGuid();

        h.Crm.PublishAccountUpdated(new CrmAccount { AccountId = id, Name = "No Number" });

        Assert.Equal($"CustomerId: {id}, Name: No Number, Email: , Phone: ", SentBy(h, "CRM").Summary);
    }



    [Theory]
    [InlineData(AccountStatusCode.Active, "account.activated")]
    [InlineData(AccountStatusCode.Inactive, "account.deactivated")]
    public async Task CrmConnector_PublishStatusChanged_publishes_the_topic_for_the_new_status(AccountStatusCode status, string expectedTopic)
    {
        using var h = await ConnectorHarness.CreateAsync();

        h.Crm.PublishStatusChanged(new CrmAccount { AccountId = Guid.NewGuid(), Name = "Acme", StatusCode = status });

        Assert.Equal(expectedTopic, SentBy(h, "CRM").Topic);
    }



    [Fact]
    public async Task ErpConnector_publish_methods_send_the_ERP_customer_under_its_customer_number()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var cust = new CustMast { CustNum = "CUST0001", Name = "Acme" };

        h.Erp.PublishAccountUpdated(cust);
        h.Erp.PublishStatusChanged(cust, wasActive: true);
        h.Erp.PublishStatusChanged(cust, wasActive: false);
        h.Erp.PublishNew(cust);

        var sent = h.Monitor.Entries.Where(e => string.Equals(e.AppName, "ERP", StringComparison.Ordinal) && string.Equals(e.Direction, "→ SEND", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { "account.updated", "account.deactivated", "account.activated", "account.created" }, sent.Select(e => e.Topic));
        Assert.All(sent, e => Assert.Equal("CUST0001", e.CorrelationId));
        Assert.Equal("CustomerId: CUST0001, Name: Acme, Email: , Phone: ", sent[0].Summary);
    }



    [Fact]
    public async Task ECommerce_and_Portal_PublishAccountUpdated_send_the_customer_under_its_customer_number()
    {
        using var h = await ConnectorHarness.CreateAsync();

        h.ECommerce.PublishAccountUpdated(new ECommerceCustomer { CustNum = "CUST0002", Name = "Globex" });
        h.Portal.PublishAccountUpdated(new PortalCustomer { CustNum = "CUST0003", Name = "Initech", Email = "i@initech.com", Phone = "555-3" });

        Assert.Equal("CustomerId: CUST0002, Name: Globex, Email: , Phone: ", SentBy(h, "eCommerce").Summary);
        Assert.Equal("CustomerId: CUST0003, Name: Initech, Email: i@initech.com, Phone: 555-3", SentBy(h, "Customer Portal").Summary);
    }



    [Fact]
    public async Task Inbound_account_updated_changes_the_customer_in_every_subscribing_system()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var changed = new List<string>();
        h.Crm.DataChanged += (_, _) => changed.Add("CRM");
        h.Erp.DataChanged += (_, _) => changed.Add("ERP");
        h.ECommerce.DataChanged += (_, _) => changed.Add("eCommerce");
        h.Portal.DataChanged += (_, _) => changed.Add("Portal");
        h.Warehouse.DataChanged += (_, _) => changed.Add("DW");

        h.Monitor.Publish
        (
            "account.updated",
            "Tester",
            "u-1",
            Fields(("CustomerId", "CUST0001"), ("Name", "Acme Renamed"), ("Email", "new@acme.com"), ("Phone", "555-9999"))
        );
        await TestConfig.WaitUntilAsync(() => changed.Count == 5);

        var crm = h.Query<CrmDbContext, CrmAccount>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0001"));
        Assert.Equal(("Acme Renamed", "new@acme.com", "555-9999"), (crm.Name, crm.EMailAddress1, crm.Telephone1));
        var erp = h.Query<ErpDbContext, CustMast>(db => db.CustMast.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("Acme Renamed", "new@acme.com", "555-9999"), (erp.Name, erp.Email, erp.Phone));
        var shop = h.Query<ECommerceDbContext, ECommerceCustomer>(db => db.Customers.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("Acme Renamed", "new@acme.com", "555-9999"), (shop.Name, shop.Email, shop.Phone));
        var portal = h.Query<CustomerPortalDbContext, PortalCustomer>(db => db.Customers.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("Acme Renamed", "new@acme.com", "555-9999"), (portal.Name, portal.Email, portal.Phone));
        var dw = h.Query<DataWarehouseDbContext, DwCustomer>(db => db.Customers.Single(c => c.CustNum == "CUST0001"));
        Assert.Equal(("Acme Renamed", "new@acme.com", "555-9999"), (dw.Name, dw.Email, dw.Phone));
    }



    [Fact]
    public async Task Inbound_account_deactivated_then_activated_toggles_every_systems_status()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var changed = 0;
        h.Crm.DataChanged += (_, _) => changed++;
        h.Erp.DataChanged += (_, _) => changed++;
        h.ECommerce.DataChanged += (_, _) => changed++;
        h.Portal.DataChanged += (_, _) => changed++;
        h.Warehouse.DataChanged += (_, _) => changed++;

        h.Monitor.Publish("account.deactivated", "Tester", "d-1", Fields(("CustomerId", "CUST0002"), ("Name", "Globex")));
        await TestConfig.WaitUntilAsync(() => changed == 5);

        Assert.Equal(AccountStatusCode.Inactive, h.Query<CrmDbContext, AccountStatusCode>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0002").StatusCode));
        Assert.False(h.Query<ErpDbContext, bool>(db => db.CustMast.Single(c => c.CustNum == "CUST0002").IsActive));
        Assert.False(h.Query<ECommerceDbContext, bool>(db => db.Customers.Single(c => c.CustNum == "CUST0002").IsActive));
        Assert.False(h.Query<CustomerPortalDbContext, bool>(db => db.Customers.Single(c => c.CustNum == "CUST0002").IsActive));
        Assert.False(h.Query<DataWarehouseDbContext, bool>(db => db.Customers.Single(c => c.CustNum == "CUST0002").IsActive));

        h.Monitor.Publish("account.activated", "Tester", "a-1", Fields(("CustomerId", "CUST0002"), ("Name", "Globex")));
        await TestConfig.WaitUntilAsync(() => changed == 10);

        Assert.Equal(AccountStatusCode.Active, h.Query<CrmDbContext, AccountStatusCode>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0002").StatusCode));
        Assert.True(h.Query<ErpDbContext, bool>(db => db.CustMast.Single(c => c.CustNum == "CUST0002").IsActive));
        Assert.True(h.Query<ECommerceDbContext, bool>(db => db.Customers.Single(c => c.CustNum == "CUST0002").IsActive));
        Assert.True(h.Query<CustomerPortalDbContext, bool>(db => db.Customers.Single(c => c.CustNum == "CUST0002").IsActive));
        Assert.True(h.Query<DataWarehouseDbContext, bool>(db => db.Customers.Single(c => c.CustNum == "CUST0002").IsActive));
    }



    [Fact]
    public async Task Inbound_account_created_adds_a_new_customer_to_every_receiving_system_and_ignores_known_ones()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var changed = 0;
        h.Crm.DataChanged += (_, _) => changed++;
        h.ECommerce.DataChanged += (_, _) => changed++;
        h.Portal.DataChanged += (_, _) => changed++;
        h.Warehouse.DataChanged += (_, _) => changed++;

        h.Monitor.Publish("account.created", "Tester", "c-1", Fields(("CustomerId", "CUST0042"), ("Email", "n@new.com"), ("Phone", "555-42")));
        await TestConfig.WaitUntilAsync(() => changed == 4);
        h.Monitor.Publish("account.created", "Tester", "c-2", Fields(("CustomerId", "CUST0001"), ("Name", "Ignored")));

        Assert.Equal("CUST0042", h.Query<CrmDbContext, string>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0042").Name));
        Assert.Equal("CUST0042", h.Query<ECommerceDbContext, string>(db => db.Customers.Single(c => c.CustNum == "CUST0042").Name));
        Assert.Equal("n@new.com", h.Query<CustomerPortalDbContext, string?>(db => db.Customers.Single(c => c.CustNum == "CUST0042").Email));
        Assert.Equal("555-42", h.Query<DataWarehouseDbContext, string?>(db => db.Customers.Single(c => c.CustNum == "CUST0042").Phone));
        Assert.Equal(4, changed);
        Assert.Equal("Acme Corporation", h.Query<DataWarehouseDbContext, string>(db => db.Customers.Single(c => c.CustNum == "CUST0001").Name));
    }



    [Fact]
    public async Task Inbound_account_updated_for_an_unknown_customer_is_created_only_by_the_CRM()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var crmChanged = 0;
        var othersChanged = 0;
        h.Crm.DataChanged += (_, _) => crmChanged++;
        h.Erp.DataChanged += (_, _) => othersChanged++;
        h.ECommerce.DataChanged += (_, _) => othersChanged++;
        h.Portal.DataChanged += (_, _) => othersChanged++;
        h.Warehouse.DataChanged += (_, _) => othersChanged++;

        h.Monitor.Publish("account.updated", "Tester", "u-1", Fields(("CustomerId", "CUST0077"), ("Name", "Late Co")));
        await TestConfig.WaitUntilAsync(() => crmChanged == 1);

        Assert.Equal("Late Co", h.Query<CrmDbContext, string>(db => db.Accounts.Single(a => a.AccountNumber == "CUST0077").Name));
        Assert.Equal(0, othersChanged);
        Assert.False(h.Query<ErpDbContext, bool>(db => db.CustMast.Any(c => c.CustNum == "CUST0077")));
        Assert.False(h.Query<DataWarehouseDbContext, bool>(db => db.Customers.Any(c => c.CustNum == "CUST0077")));
    }



    [Fact]
    public async Task Inbound_status_change_for_an_unknown_customer_changes_nothing()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var changed = 0;
        h.Crm.DataChanged += (_, _) => changed++;
        h.Erp.DataChanged += (_, _) => changed++;
        h.ECommerce.DataChanged += (_, _) => changed++;
        h.Portal.DataChanged += (_, _) => changed++;
        h.Warehouse.DataChanged += (_, _) => changed++;

        h.Monitor.Publish("account.deactivated", "Tester", "d-1", Fields(("CustomerId", "CUST0088")));
        h.Monitor.Publish("account.activated", "Tester", "a-1", Fields(("CustomerId", "CUST0088")));
        h.Monitor.Publish("account.updated", "Tester", "u-1", Fields(("CustomerId", "CUST0001")));
        await TestConfig.WaitUntilAsync(() => changed == 5);

        Assert.Equal(5, changed);
        Assert.False(h.Query<CrmDbContext, bool>(db => db.Accounts.Any(a => a.AccountNumber == "CUST0088")));
    }



    [Fact]
    public async Task Inbound_message_without_a_customer_id_or_with_an_unhandled_topic_changes_nothing()
    {
        using var h = await ConnectorHarness.CreateAsync();
        var changed = 0;
        h.Crm.DataChanged += (_, _) => changed++;
        h.Erp.DataChanged += (_, _) => changed++;
        h.ECommerce.DataChanged += (_, _) => changed++;
        h.Portal.DataChanged += (_, _) => changed++;
        h.Warehouse.DataChanged += (_, _) => changed++;

        h.Monitor.Publish("account.updated", "Tester", "u-1", Fields(("Name", "No Id")));
        h.Monitor.Publish("account.updated", "Tester", "u-2", Fields(("CustomerId", string.Empty)));
        h.Monitor.Publish("order.shipped", "Tester", "o-1", Fields(("CustomerId", "CUST0001"), ("OrderId", "ERP-1")));
        h.Monitor.Publish("order.place", "Tester", "o-2", Fields(("CustomerId", "CUST0001"), ("OrderRef", "ORD-1")));
        h.Monitor.Publish("account.created", "Tester", "c-1", Fields(("CustomerId", "CUST0001")));
        h.Monitor.Publish("account.updated", "Tester", "u-3", Fields(("CustomerId", "CUST0001")));
        await TestConfig.WaitUntilAsync(() => changed == 5);

        Assert.Equal(5, changed);
    }



    [Fact]
    public async Task ERP_auto_response_saves_the_minted_customer_before_the_other_systems_receive_it()
    {
        using var h = await ConnectorHarness.CreateAsync();

        // Each connector raises DataChanged after it has saved, so the order of the events is the
        // order the systems stored the customer. The handlers capture only this list.
        var saved = new List<string>();
        h.Erp.DataChanged += (_, _) => saved.Add("ERP");
        h.Crm.DataChanged += (_, _) => saved.Add("CRM");

        h.Monitor.Publish("account.create", "CRM", "corr-1", Fields(("Name", "Acme Corp 7"), ("Email", "a7@acme.com")), isUserInitiated: true);
        h.Time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => saved.Contains("CRM"));

        Assert.Equal(new[] { "ERP", "CRM" }, saved);
        var mintedId = h.Query<CrmDbContext, string?>(db => db.Accounts.Single(a => a.Name == "Acme Corp 7").AccountNumber);
        var erp = h.Query<ErpDbContext, CustMast>(db => db.CustMast.Single(c => c.CustNum == mintedId));
        Assert.Equal(("Acme Corp 7", "a7@acme.com"), (erp.Name, erp.Email));
    }



    [Fact]
    public async Task Seeded_databases_hold_the_five_well_known_customers()
    {
        using var h = await ConnectorHarness.CreateAsync();

        Assert.Equal(WellKnown.Accounts.CustNums, h.Query<CrmDbContext, string?[]>(db => db.Accounts.OrderBy(a => a.AccountNumber).Select(a => a.AccountNumber).ToArray()));
        Assert.Equal(WellKnown.Accounts.CustNums, h.Query<ErpDbContext, string[]>(db => db.CustMast.OrderBy(c => c.CustNum).Select(c => c.CustNum).ToArray()));
        Assert.Equal(WellKnown.Accounts.CustNums, h.Query<ECommerceDbContext, string[]>(db => db.Customers.OrderBy(c => c.CustNum).Select(c => c.CustNum).ToArray()));
        Assert.Equal(WellKnown.Accounts.CustNums, h.Query<CustomerPortalDbContext, string[]>(db => db.Customers.OrderBy(c => c.CustNum).Select(c => c.CustNum).ToArray()));
        Assert.Equal(WellKnown.Accounts.CustNums, h.Query<DataWarehouseDbContext, string[]>(db => db.Customers.OrderBy(c => c.CustNum).Select(c => c.CustNum).ToArray()));
        Assert.Contains(h.Query<CrmDbContext, Guid[]>(db => db.Accounts.Select(a => a.AccountId).ToArray()), id => id == WellKnown.Accounts.SoylentId);
    }
}
