using System.Text.RegularExpressions;
using EsbDemo.Web.Models;
using EsbDemo.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace EsbDemo.Web.Tests.Unit.Services;

public sealed class MonitorServiceTests
{
    private static readonly Regex CustomerIdPattern = new("^CUST[0-9]{4}$", RegexOptions.None, TimeSpan.FromSeconds(1));



    private static readonly Regex OrderIdPattern = new("OrderId: ERP-[0-9]{4}", RegexOptions.None, TimeSpan.FromSeconds(1));



    private static async Task<(MonitorService Sut, FakeTimeProvider Time)> CreateAsync()
    {
        var time = new FakeTimeProvider();
        return (new MonitorService(await TestConfig.LoadRealAsync(), time), time);
    }



    private static Dictionary<string, string> Fields(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);



    private static MonitorEntry[] Sent(MonitorService sut, string topic) =>
        sut.Entries.Where(e => string.Equals(e.Topic, topic, StringComparison.Ordinal) && string.Equals(e.Direction, "→ SEND", StringComparison.Ordinal)).ToArray();



    private static MonitorEntry? Received(MonitorService sut, string topic, string app) =>
        sut.Entries.SingleOrDefault(e => string.Equals(e.Topic, topic, StringComparison.Ordinal) && string.Equals(e.AppName, app, StringComparison.Ordinal) && string.Equals(e.Direction, "← RECV", StringComparison.Ordinal));



    [Fact]
    public void MonitorService_public_constructor_uses_the_system_clock_and_the_mock_bus()
    {
        var sut = new MonitorService(new ConfigService(new FakeWebHostEnvironment(AppContext.BaseDirectory)));

        Assert.Equal("Mock", sut.SelectedBus);
        Assert.Empty(sut.Entries);
        Assert.Empty(sut.Accounts);
    }



    [Fact]
    public async Task MonitorService_when_an_app_defaults_offline_starts_it_offline()
    {
        var config = await TestConfig.LoadAsync
        (
            """[ { "name": "Sleepy", "defaultOnline": false }, { "name": "Awake" } ]""",
            "[]"
        );

        var sut = new MonitorService(config, new FakeTimeProvider());

        Assert.False(sut.IsOnline("Sleepy"));
        Assert.True(sut.IsOnline("Awake"));
    }



    [Fact]
    public async Task IsOnline_when_app_is_unknown_returns_true()
    {
        var (sut, _) = await CreateAsync();

        Assert.True(sut.IsOnline("Nobody"));
    }



    [Fact]
    public async Task GetQueuedCount_when_app_is_unknown_returns_zero()
    {
        var (sut, _) = await CreateAsync();

        Assert.Equal(0, sut.GetQueuedCount("Nobody"));
    }



    [Fact]
    public async Task Publish_when_fields_is_null_throws_ArgumentNullException()
    {
        var (sut, _) = await CreateAsync();

        var ex = Assert.Throws<ArgumentNullException>(() => sut.Publish("account.updated", "CRM", null, null!));

        Assert.Equal("fields", ex.ParamName);
    }



    [Fact]
    public async Task Publish_logs_one_send_entry_and_one_receive_entry_per_subscriber()
    {
        var (sut, time) = await CreateAsync();

        sut.Publish("account.deactivated", "CRM", "c-1", Fields(("CustomerId", "CUST0001"), ("Name", "Acme")));

        var send = Assert.Single(sut.Entries, e => string.Equals(e.Direction, "→ SEND", StringComparison.Ordinal));
        Assert.Equal("CRM", send.AppName);
        Assert.Equal("c-1", send.CorrelationId);
        Assert.Equal("CustomerId: CUST0001, Name: Acme", send.Summary);
        Assert.Equal("topic-red-dark", send.CssClass);
        Assert.Equal(time.GetUtcNow(), send.Timestamp);
        Assert.Equal
        (
            new[] { "CRM", "ERP", "eCommerce", "Customer Portal", "Data Warehouse" },
            sut.Entries.Where(e => string.Equals(e.Direction, "← RECV", StringComparison.Ordinal)).Select(e => e.AppName)
        );
    }



    [Fact]
    public async Task Publish_when_config_loaded_after_construction_still_queues_for_offline_apps()
    {
        var config = new ConfigService(new FakeWebHostEnvironment(AppContext.BaseDirectory));
        var sut = new MonitorService(config, new FakeTimeProvider());
        await config.LoadAsync();
        sut.SetOnline("ERP", online: false);

        sut.Publish("account.updated", "CRM", "c-1", Fields(("CustomerId", "CUST0001")));

        Assert.Equal(1, sut.GetQueuedCount("ERP"));
    }



    [Fact]
    public async Task Publish_when_the_source_also_subscribes_logs_its_copy_as_self_originated()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.deactivated", "CRM", "c-1", Fields(("CustomerId", "CUST0001"), ("Name", "Acme")));

        var self = Received(sut, "account.deactivated", "CRM")!;
        Assert.Equal("Self-originated — no action taken", self.Summary);
        Assert.Equal("topic-self", self.CssClass);
        Assert.Equal("Customer CUST0001 (Acme) marked inactive", Received(sut, "account.deactivated", "ERP")!.Summary);
    }



    [Fact]
    public async Task Publish_raises_OnChanged()
    {
        var (sut, _) = await CreateAsync();
        var raised = 0;
        sut.OnChanged += (_, _) => raised++;

        sut.Publish("account.activated", "CRM", "c-1", Fields(("CustomerId", "CUST0001"), ("Name", "Acme")));

        Assert.Equal(1, raised);
        Assert.Equal("Customer CUST0001 (Acme) re-activated", Received(sut, "account.activated", "ERP")!.Summary);
    }



    [Fact]
    public async Task Publish_passes_each_subscriber_handler_its_own_copy_and_skips_the_source()
    {
        var (sut, _) = await CreateAsync();
        var fields = Fields(("CustomerId", "CUST0001"), ("Name", "Acme"));
        var erpCalls = new List<(string Topic, Dictionary<string, string> Fields)>();
        var crmCalls = 0;
        sut.RegisterHandler("ERP", (topic, f) => { erpCalls.Add((topic, f)); return Task.CompletedTask; });
        sut.RegisterHandler("CRM", (_, _) => { crmCalls++; return Task.CompletedTask; });

        sut.Publish("account.updated", "CRM", "c-1", fields);

        var call = Assert.Single(erpCalls);
        Assert.Equal("account.updated", call.Topic);
        Assert.Equal(fields, call.Fields);
        Assert.NotSame(fields, call.Fields);
        Assert.Equal(0, crmCalls);
    }



    [Fact]
    public async Task RegisterHandler_when_called_again_replaces_the_previous_handler()
    {
        var (sut, _) = await CreateAsync();
        var first = 0;
        var second = 0;
        sut.RegisterHandler("ERP", (_, _) => { first++; return Task.CompletedTask; });
        sut.RegisterHandler("ERP", (_, _) => { second++; return Task.CompletedTask; });

        sut.Publish("account.updated", "CRM", "c-1", Fields(("CustomerId", "CUST0001")));

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }



    [Fact]
    public async Task Publish_when_a_subscriber_is_offline_queues_the_message_instead_of_delivering_it()
    {
        var (sut, _) = await CreateAsync();
        var handled = 0;
        sut.RegisterHandler("ERP", (_, _) => { handled++; return Task.CompletedTask; });
        sut.SetOnline("ERP", online: false);

        sut.Publish("account.updated", "CRM", "c-1", Fields(("CustomerId", "CUST0001")));

        Assert.False(sut.IsOnline("ERP"));
        Assert.Equal(1, sut.GetQueuedCount("ERP"));
        Assert.Null(Received(sut, "account.updated", "ERP"));
        Assert.Equal(0, handled);
    }



    [Fact]
    public async Task SetOnline_when_an_app_comes_back_delivers_its_queue_in_order_with_the_original_times()
    {
        var (sut, time) = await CreateAsync();
        var handled = new List<string>();
        sut.RegisterHandler("ERP", (_, f) => { handled.Add(f["CustomerId"]); return Task.CompletedTask; });
        sut.SetOnline("ERP", online: false);
        var sentAt = time.GetUtcNow();
        sut.Publish("account.updated", "CRM", "c-1", Fields(("CustomerId", "CUST0001")));
        sut.Publish("account.updated", "CRM", "c-2", Fields(("CustomerId", "CUST0002")));
        time.Advance(TimeSpan.FromMinutes(1));
        var raised = 0;
        sut.OnChanged += (_, _) => raised++;

        sut.SetOnline("ERP", online: true);

        Assert.Equal(new[] { "CUST0001", "CUST0002" }, handled);
        Assert.Equal(0, sut.GetQueuedCount("ERP"));
        var delivered = sut.Entries.Where(e => string.Equals(e.AppName, "ERP", StringComparison.Ordinal) && string.Equals(e.Direction, "← RECV", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { "c-1", "c-2" }, delivered.Select(e => e.CorrelationId));
        Assert.All(delivered, e => Assert.Equal(sentAt, e.Timestamp));
        Assert.Equal("CustomerId: CUST0001", delivered[0].Summary);
        Assert.Equal(1, raised);
    }



    [Fact]
    public async Task SetOnline_marks_only_the_deliveries_that_waited_in_the_queue_as_queued()
    {
        var (sut, _) = await CreateAsync();
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.updated", "CRM", "c-1", Fields(("CustomerId", "CUST0001")));

        sut.SetOnline("ERP", online: true);

        Assert.True(Received(sut, "account.updated", "ERP")!.IsQueued);
        Assert.False(Received(sut, "account.updated", "eCommerce")!.IsQueued);
        Assert.False(Sent(sut, "account.updated")[0].IsQueued);
    }



    [Fact]
    public async Task SetOnline_when_a_queued_message_came_from_the_app_itself_does_not_call_its_handler()
    {
        var (sut, _) = await CreateAsync();
        var handled = 0;
        sut.RegisterHandler("ERP", (_, _) => { handled++; return Task.CompletedTask; });
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.updated", "ERP", "c-1", Fields(("CustomerId", "CUST0001")));

        sut.SetOnline("ERP", online: true);

        Assert.Equal(0, handled);
        Assert.NotNull(Received(sut, "account.updated", "ERP"));
    }



    [Fact]
    public async Task Publish_when_a_user_creates_an_account_the_ERP_replies_with_a_minted_customer_id()
    {
        var (sut, time) = await CreateAsync();
        var fields = Fields(("Name", "Acme Corp 7"), ("Email", "a@acme.com"), ("Phone", "555-0001"));

        sut.Publish("account.create", "CRM", "corr-1", fields, isUserInitiated: true);

        var pending = Assert.Single(sut.Accounts);
        Assert.Equal("Pending", pending.Status);
        Assert.Equal("CRM", pending.SourceApp);
        Assert.Null(pending.CustomerId);

        time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => Sent(sut, "account.created").Length == 1);

        var reply = Sent(sut, "account.created")[0];
        Assert.Equal("ERP", reply.AppName);
        Assert.Equal("corr-1", reply.CorrelationId);
        Assert.Contains("Name: Acme Corp 7", reply.Summary, StringComparison.Ordinal);
        var account = Assert.Single(sut.Accounts);
        Assert.Equal("Active", account.Status);
        Assert.Matches(CustomerIdPattern, account.CustomerId);
        Assert.Equal("Acme Corp 7", account.Fields["Name"]);

        time.Advance(TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(sut.Entries, e => string.Equals(e.Direction, "⚠ TIMEOUT", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Publish_when_the_ERP_does_not_reply_within_10_seconds_marks_the_account_pending_create_finish()
    {
        var (sut, time) = await CreateAsync();
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.create", "CRM", "corr-1", Fields(("Name", "Acme")), isUserInitiated: true);
        var raised = 0;
        sut.OnChanged += (_, _) => raised++;

        time.Advance(TimeSpan.FromSeconds(10));
        await TestConfig.WaitUntilAsync(() => sut.Entries.Any(e => string.Equals(e.Direction, "⚠ TIMEOUT", StringComparison.Ordinal)));

        var timeout = sut.Entries.Single(e => string.Equals(e.Direction, "⚠ TIMEOUT", StringComparison.Ordinal));
        Assert.Equal("CRM", timeout.AppName);
        Assert.Equal("topic-timeout", timeout.CssClass);
        Assert.Equal("Pending Create Finish", Assert.Single(sut.Accounts).Status);
        Assert.Equal(1, raised);

        sut.SetOnline("ERP", online: true);
        time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => string.Equals(sut.Accounts.Single().Status, "Active", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Publish_when_account_create_has_no_correlation_id_does_not_track_it()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.create", "CRM", null, Fields(("Name", "Acme")));

        Assert.Empty(sut.Accounts);
    }



    [Fact]
    public async Task Publish_when_not_user_initiated_does_not_trigger_auto_responses()
    {
        var (sut, time) = await CreateAsync();

        sut.Publish("account.create", "CRM", "corr-1", Fields(("Name", "Acme")), isUserInitiated: false);
        time.Advance(TimeSpan.FromSeconds(1));

        Assert.Empty(Sent(sut, "account.created"));
    }



    [Fact]
    public async Task Publish_when_the_ERP_replies_to_its_own_request_uses_the_minted_id_as_correlation_id()
    {
        var (sut, time) = await CreateAsync();
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.create", "ERP", "erp-corr", Fields(("Name", "Own")), isUserInitiated: true);

        sut.SetOnline("ERP", online: true);
        time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => Sent(sut, "account.created").Length == 1);

        var reply = Sent(sut, "account.created")[0];
        Assert.Matches(CustomerIdPattern, reply.CorrelationId);
        Assert.Contains("CustomerId: " + reply.CorrelationId, reply.Summary, StringComparison.Ordinal);
    }



    [Fact]
    public async Task Publish_when_an_order_is_placed_the_reply_keeps_the_request_values_and_correlation()
    {
        var (sut, time) = await CreateAsync();

        sut.Publish
        (
            "order.place",
            "eCommerce",
            "ord-1",
            Fields(("CustomerId", "CUST0001"), ("OrderRef", "ORD-1234"), ("Amount", "$1,230.00")),
            isUserInitiated: true
        );
        time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => Sent(sut, "order.placed").Length == 1);

        var reply = Sent(sut, "order.placed")[0];
        Assert.Equal("ERP", reply.AppName);
        Assert.Equal("ord-1", reply.CorrelationId);
        Assert.Contains("OrderRef: ORD-1234", reply.Summary, StringComparison.Ordinal);
        Assert.Matches(OrderIdPattern, reply.Summary);
    }



    [Fact]
    public async Task Publish_when_an_auto_response_would_republish_the_same_topic_skips_it()
    {
        var config = await TestConfig.LoadAsync
        (
            """
            [
              { "name": "Echo",   "subscribes": [ "ping" ],  "autoResponses": [ { "on": "ping",  "publish": "ping" } ] },
              { "name": "Ponger", "subscribes": [ "ping2" ], "autoResponses": [ { "on": "ping2", "publish": "pong" } ] }
            ]
            """,
            "[]"
        );
        var time = new FakeTimeProvider();
        var sut = new MonitorService(config, time);

        sut.Publish("ping", "Tester", "p-1", Fields(("A", "1")), isUserInitiated: true);
        sut.Publish("ping2", "Tester", "p-2", Fields(("A", "2")), isUserInitiated: true);
        time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => Sent(sut, "pong").Length == 1);

        Assert.Single(Sent(sut, "ping"));
        Assert.Equal("A: 2", Sent(sut, "pong")[0].Summary);
        Assert.Equal("topic-default", Sent(sut, "pong")[0].CssClass);
    }



    [Fact]
    public async Task Publish_when_an_app_auto_responds_its_own_handler_sees_the_reply_before_anyone_else()
    {
        var (sut, time) = await CreateAsync();
        var seen = new List<(string Topic, bool AlreadyPublished)>();
        sut.RegisterHandler
        (
            "ERP",
            (topic, _) =>
            {
                seen.Add((topic, Sent(sut, "account.created").Length > 0));
                return Task.CompletedTask;
            }
        );

        sut.Publish("account.create", "CRM", "corr-1", Fields(("Name", "Acme")), isUserInitiated: true);
        time.Advance(TimeSpan.FromMilliseconds(500));
        await TestConfig.WaitUntilAsync(() => Sent(sut, "account.created").Length == 1);

        Assert.Equal(new[] { ("account.create", false), ("account.created", false) }, seen);
    }



    [Fact]
    public async Task Publish_when_account_created_arrives_without_a_pending_request_tracks_the_account()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.created", "ERP", "CUST0099", Fields(("CustomerId", "CUST0099"), ("Name", "New Co")));

        var account = Assert.Single(sut.Accounts);
        Assert.Equal("CUST0099", account.CustomerId);
        Assert.Equal("Active", account.Status);
        Assert.Equal(string.Empty, account.SourceApp);
        Assert.Equal
        (
            "Account record updated — AccountNumber set to CUST0099",
            Received(sut, "account.created", "CRM")!.Summary
        );
        Assert.Equal
        (
            "Inserted new record — Account CUST0099: New Co",
            Received(sut, "account.created", "Data Warehouse")!.Summary
        );
    }



    [Fact]
    public async Task Publish_when_account_created_has_no_correlation_id_tracks_it_by_customer_id_only()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.created", "ERP", null, Fields(("CustomerId", "CUST0099"), ("Name", "New Co")));
        sut.Publish("account.updated", "CRM", "u-1", Fields(("CustomerId", "CUST0099"), ("Name", "Renamed Co")));

        Assert.Empty(sut.Accounts);
        Assert.Equal("Name: \"New Co\" → \"Renamed Co\"", Received(sut, "account.updated", "ERP")!.Summary);
    }



    [Fact]
    public async Task Publish_when_account_created_has_no_customer_id_ignores_it()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.created", "ERP", "c-1", Fields(("Name", "No Id")));

        Assert.Empty(sut.Accounts);
        Assert.Equal("Account record updated — AccountNumber set to ?", Received(sut, "account.created", "CRM")!.Summary);
    }



    [Fact]
    public async Task Publish_when_an_unknown_account_is_updated_starts_tracking_it_and_later_updates_show_the_changes()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.updated", "CRM", "u-1", Fields(("CustomerId", "CUST0001"), ("Email", "old@acme.com")));
        sut.Publish("account.updated", "CRM", "u-2", Fields(("CustomerId", "CUST0001"), ("Email", "new@acme.com")));
        sut.Publish("account.updated", "CRM", "u-3", Fields(("CustomerId", "CUST0001"), ("Email", "new@acme.com")));

        var summaries = sut.Entries
            .Where(e => string.Equals(e.AppName, "ERP", StringComparison.Ordinal) && string.Equals(e.Direction, "← RECV", StringComparison.Ordinal))
            .Select(e => e.Summary)
            .ToArray();
        Assert.Equal
        (
            new[]
            {
                "CustomerId: CUST0001, Email: old@acme.com",
                "Email: \"old@acme.com\" → \"new@acme.com\"",
                "CustomerId: CUST0001, Email: new@acme.com"
            },
            summaries
        );
    }



    [Fact]
    public async Task Publish_when_an_update_has_no_customer_id_does_not_track_it()
    {
        var (sut, _) = await CreateAsync();

        sut.Publish("account.updated", "CRM", "u-1", Fields(("Name", "Nobody")));
        sut.Publish("account.updated", "CRM", "u-2", Fields(("CustomerId", string.Empty)));

        Assert.Empty(sut.Accounts);
    }



    [Fact]
    public async Task SetBus_resets_the_log_accounts_queues_and_pending_timeouts()
    {
        var (sut, time) = await CreateAsync();
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.create", "CRM", "corr-1", Fields(("Name", "Acme")), isUserInitiated: true);
        var raised = 0;
        sut.OnChanged += (_, _) => raised++;

        sut.SetBus("Kafka");
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal("Kafka", sut.SelectedBus);
        Assert.Empty(sut.Entries);
        Assert.Empty(sut.Accounts);
        Assert.Equal(0, sut.GetQueuedCount("ERP"));
        Assert.Equal(1, raised);
    }



    [Fact]
    public async Task Entries_and_Accounts_return_snapshots_that_later_activity_does_not_change()
    {
        var (sut, _) = await CreateAsync();
        sut.Publish("account.created", "ERP", "CUST0099", Fields(("CustomerId", "CUST0099"), ("Name", "New Co")));
        var entries = sut.Entries;
        var account = sut.Accounts[0];

        sut.Publish("account.updated", "CRM", "u-1", Fields(("CustomerId", "CUST0099"), ("Name", "Renamed Co")));
        account.Status = "Changed by caller";

        Assert.Equal(5, entries.Count);
        Assert.Equal("New Co", account.Fields["Name"]);
        Assert.Equal("Active", sut.Accounts[0].Status);
        Assert.Equal("Renamed Co", sut.Accounts[0].Fields["Name"]);
    }


    [Fact]
    public async Task Publish_when_a_handler_throws_synchronously_still_delivers_to_the_other_subscribers_and_logs_the_fault()
    {
        var logger = new FakeLogger<MonitorService>();
        var sut = new MonitorService(await TestConfig.LoadRealAsync(), new FakeTimeProvider(), logger);
        var warehouseCalls = 0;
        sut.RegisterHandler("CRM", (_, _) => throw new InvalidOperationException("CRM is broken"));
        sut.RegisterHandler("Data Warehouse", (_, _) => { warehouseCalls++; return Task.CompletedTask; });

        sut.Publish("account.created", "Tester", "c-1", Fields(("CustomerId", "CUST0001")));

        Assert.Equal(1, warehouseCalls);
        await TestConfig.WaitUntilAsync(() => logger.Collector.Count > 0);
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.IsType<InvalidOperationException>(record.Exception);
        Assert.Contains("CRM handler for account.created", record.Message, StringComparison.Ordinal);
    }



    [Fact]
    public async Task SetOnline_when_a_flushed_handler_faults_asynchronously_logs_the_fault()
    {
        var logger = new FakeLogger<MonitorService>();
        var sut = new MonitorService(await TestConfig.LoadRealAsync(), new FakeTimeProvider(), logger);
        sut.RegisterHandler("ERP", (_, _) => Task.FromException(new InvalidOperationException("ERP save failed")));
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.updated", "CRM", "c-1", Fields(("CustomerId", "CUST0001")));

        sut.SetOnline("ERP", online: true);

        await TestConfig.WaitUntilAsync(() => logger.Collector.Count > 0);
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal("ERP save failed", record.Exception?.Message);
        Assert.Contains("ERP handler for account.updated", record.Message, StringComparison.Ordinal);
    }



    [Fact]
    public async Task Publish_when_account_create_repeats_a_correlation_id_times_out_once_for_the_latest_request()
    {
        var (sut, time) = await CreateAsync();
        sut.SetOnline("ERP", online: false);
        sut.Publish("account.create", "CRM", "corr-1", Fields(("Name", "First")), isUserInitiated: true);
        sut.Publish("account.create", "eCommerce", "corr-1", Fields(("Name", "Second")), isUserInitiated: true);

        time.Advance(TimeSpan.FromSeconds(10));
        await TestConfig.WaitUntilAsync(() => sut.Entries.Any(e => string.Equals(e.Direction, "⚠ TIMEOUT", StringComparison.Ordinal)));

        var timeout = Assert.Single(sut.Entries, e => string.Equals(e.Direction, "⚠ TIMEOUT", StringComparison.Ordinal));
        Assert.Equal("eCommerce", timeout.AppName);
        Assert.Equal("Second", Assert.Single(sut.Accounts).Fields["Name"]);
    }
}
