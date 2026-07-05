using EsbDemo.Core.Config;
using EsbDemo.Core.Services;
using EsbDemo.Web.Models;

namespace EsbDemo.Web.Services;

public sealed class MonitorService
{
    private readonly ConfigService _config;
    private readonly List<MonitorEntry> _entries = new();
    private readonly Dictionary<string, bool> _onlineStatus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<QueuedMessage>> _queued = new(StringComparer.OrdinalIgnoreCase);

    // Connector handlers — keyed by app name, called when a message is delivered to that app
    private readonly Dictionary<string, Func<string, Dictionary<string, string>, Task>> _handlers
        = new(StringComparer.OrdinalIgnoreCase);

    // Account tracking
    private readonly Dictionary<string, AccountRecord> _accountsByCorrelation = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AccountRecord> _accountsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _pendingCreateCts = new(StringComparer.Ordinal);

    public string SelectedBus { get; private set; } = "Mock";


    public event Action? OnChanged;


    public IReadOnlyList<MonitorEntry> Entries => _entries;


    public IReadOnlyList<AccountRecord> Accounts => [.. _accountsByCorrelation.Values];


    public MonitorService(ConfigService config)
    {
        _config = config;
        InitializeAppStatus();
    }


    /// <summary>
    /// Registers a connector handler that is called whenever a message is delivered to
    /// <paramref name="appName"/>. The handler is NOT called when the app itself is the source,
    /// preventing loops.
    /// </summary>
    public void RegisterHandler(string appName, Func<string, Dictionary<string, string>, Task> handler) =>
        _handlers[appName] = handler;


    public bool IsOnline(string appName) =>
        _onlineStatus.TryGetValue(appName, out var online) ? online : true;


    public int GetQueuedCount(string appName) =>
        _queued.TryGetValue(appName, out var q) ? q.Count : 0;


    public void SetOnline(string appName, bool online)
    {
        _onlineStatus[appName] = online;

        if (online && _queued.TryGetValue(appName, out var queue))
        {
            var app = _config.Apps.FirstOrDefault(
                a => string.Equals(a.Name, appName, StringComparison.OrdinalIgnoreCase)
            );

            while (queue.TryDequeue(out var msg))
            {
                FlushQueuedMessage(app, appName, msg);
            }
        }

        NotifyChanged();
    }


    public void SetBus(string bus)
    {
        SelectedBus = bus;
        _entries.Clear();

        foreach (var cts in _pendingCreateCts.Values)
            cts.Cancel();

        _pendingCreateCts.Clear();
        _accountsByCorrelation.Clear();
        _accountsById.Clear();

        foreach (var q in _queued.Values)
            q.Clear();

        NotifyChanged();
    }


    public void Publish(
        string topic,
        string sourceApp,
        string? correlationId,
        Dictionary<string, string> fields,
        bool isUserInitiated = false)
    {
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        var now = DateTimeOffset.UtcNow;

        TrackAccountCreate(topic, sourceApp, correlationId, fields);

        var oldAccountFields = CaptureOldAccountFields(topic, fields);

        var summary = BuildSummary(fields);
        var cssClass = _config.GetTopicCssClass(topic);

        _entries.Add(
            new MonitorEntry
            {
                Timestamp = now,
                AppName = sourceApp,
                Direction = "→ SEND",
                Topic = topic,
                CorrelationId = correlationId,
                Summary = summary,
                CssClass = cssClass
            }
        );

        ResolveAccountCreated(topic, correlationId, fields);
        UpdateAccountOnUpdated(topic, fields);

        foreach (var app in _config.Apps.Where(a => a.Subscribes.Contains(topic)))
        {
            DeliverToSubscriber(
                app, topic, sourceApp, correlationId, fields,
                summary, cssClass, now, isUserInitiated, oldAccountFields
            );
        }

        NotifyChanged();
    }


    private void TrackAccountCreate(string topic, string sourceApp, string? correlationId, Dictionary<string, string> fields)
    {
        if (!string.Equals(topic, "account.create", StringComparison.Ordinal) ||
            string.IsNullOrEmpty(correlationId))
        {
            return;
        }

        var record = new AccountRecord { CorrelationId = correlationId, SourceApp = sourceApp, Status = "Pending" };
        foreach (var kv in fields) record.Fields[kv.Key] = kv.Value;
        _accountsByCorrelation[correlationId] = record;

        var cts = new CancellationTokenSource();
        _pendingCreateCts[correlationId] = cts;
        _ = WatchAccountCreateTimeoutAsync(correlationId, cts.Token);
    }


    private void ResolveAccountCreated(string topic, string? correlationId, Dictionary<string, string> fields)
    {
        if (!string.Equals(topic, "account.created", StringComparison.Ordinal) ||
            !fields.TryGetValue("CustomerId", out var customerId) ||
            string.IsNullOrEmpty(customerId))
        {
            return;
        }

        if (!string.IsNullOrEmpty(correlationId) &&
            _pendingCreateCts.TryGetValue(correlationId, out var cts))
        {
            cts.Cancel();
            _pendingCreateCts.Remove(correlationId);
        }

        AccountRecord? record = null;
        if (!string.IsNullOrEmpty(correlationId))
            _accountsByCorrelation.TryGetValue(correlationId, out record);

        if (record is not null)
        {
            record.CustomerId = customerId;
            record.Status = "Active";
            foreach (var kv in fields) record.Fields[kv.Key] = kv.Value;
        }
        else
        {
            record = new AccountRecord
            {
                CorrelationId = correlationId ?? string.Empty,
                SourceApp = string.Empty,
                CustomerId = customerId,
                Status = "Active"
            };
            foreach (var kv in fields) record.Fields[kv.Key] = kv.Value;

            if (!string.IsNullOrEmpty(correlationId))
                _accountsByCorrelation[correlationId] = record;
        }

        _accountsById[customerId] = record;
    }


    private Dictionary<string, string>? CaptureOldAccountFields(string topic, Dictionary<string, string> fields)
    {
        if (!string.Equals(topic, "account.updated", StringComparison.Ordinal) ||
            !fields.TryGetValue("CustomerId", out var id) ||
            !_accountsById.TryGetValue(id, out var existing))
        {
            return null;
        }

        return new Dictionary<string, string>(existing.Fields);
    }


    private void UpdateAccountOnUpdated(string topic, Dictionary<string, string> fields)
    {
        if (!string.Equals(topic, "account.updated", StringComparison.Ordinal) ||
            !fields.TryGetValue("CustomerId", out var id) ||
            string.IsNullOrEmpty(id))
        {
            return;
        }

        if (!_accountsById.TryGetValue(id, out var record))
        {
            record = new AccountRecord
            {
                CorrelationId = string.Empty,
                SourceApp = string.Empty,
                CustomerId = id,
                Status = "Active"
            };
            _accountsById[id] = record;
        }

        foreach (var kv in fields) record.Fields[kv.Key] = kv.Value;
    }


    private void DeliverToSubscriber(
        AppConfig app,
        string topic,
        string sourceApp,
        string? correlationId,
        Dictionary<string, string> fields,
        string sendSummary,
        string cssClass,
        DateTimeOffset now,
        bool isUserInitiated,
        Dictionary<string, string>? oldAccountFields = null)
    {
        var isSelfOrigin = string.Equals(app.Name, sourceApp, StringComparison.OrdinalIgnoreCase);

        var recvEntry = new MonitorEntry
        {
            Timestamp = now,
            AppName = app.Name,
            Direction = "← RECV",
            Topic = topic,
            CorrelationId = correlationId,
            Summary = isSelfOrigin
                ? "Self-originated — no action taken"
                : BuildReceiverSummary(app.Name, topic, fields, oldAccountFields) ?? sendSummary,
            CssClass = isSelfOrigin ? "topic-self" : cssClass
        };

        if (IsOnline(app.Name))
        {
            _entries.Add(recvEntry);

            if (!isSelfOrigin)
            {
                TriggerAutoResponse(app, topic, correlationId, fields, isUserInitiated, sourceApp);

                if (_handlers.TryGetValue(app.Name, out var handler))
                    _ = handler(topic, new Dictionary<string, string>(fields));
            }
        }
        else
        {
            if (!_queued.ContainsKey(app.Name))
                _queued[app.Name] = new Queue<QueuedMessage>();

            _queued[app.Name].Enqueue(new QueuedMessage
            {
                Topic = topic,
                SourceApp = sourceApp,
                CorrelationId = correlationId,
                Fields = new Dictionary<string, string>(fields),
                IsUserInitiated = isUserInitiated,
                Timestamp = now
            });
        }
    }


    private void FlushQueuedMessage(AppConfig? app, string appName, QueuedMessage msg)
    {
        var cssClass = _config.GetTopicCssClass(msg.Topic);
        var summary = BuildReceiverSummary(appName, msg.Topic, msg.Fields) ?? BuildSummary(msg.Fields);

        _entries.Add(
            new MonitorEntry
            {
                Timestamp = msg.Timestamp,
                AppName = appName,
                Direction = "← RECV",
                Topic = msg.Topic,
                CorrelationId = msg.CorrelationId,
                Summary = summary,
                CssClass = cssClass
            }
        );

        if (app is not null)
            TriggerAutoResponse(app, msg.Topic, msg.CorrelationId, msg.Fields, msg.IsUserInitiated, msg.SourceApp);

        if (!string.Equals(appName, msg.SourceApp, StringComparison.OrdinalIgnoreCase) &&
            _handlers.TryGetValue(appName, out var handler))
        {
            _ = handler(msg.Topic, new Dictionary<string, string>(msg.Fields));
        }
    }


    private void TriggerAutoResponse(
        AppConfig app,
        string topic,
        string? correlationId,
        Dictionary<string, string> fields,
        bool isUserInitiated,
        string originalSourceApp)
    {
        if (!isUserInitiated) return;

        var autoResponse = app.AutoResponses.FirstOrDefault(
            r => string.Equals(r.On, topic, StringComparison.Ordinal)
        );

        if (autoResponse is not null &&
            !string.Equals(autoResponse.Publish, topic, StringComparison.Ordinal))
        {
            _ = PublishAutoResponseAsync(autoResponse.Publish, app.Name, correlationId, fields, originalSourceApp);
        }
    }


    private async Task WatchAccountCreateTimeoutAsync(string correlationId, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);

            if (_accountsByCorrelation.TryGetValue(correlationId, out var record) &&
                string.Equals(record.Status, "Pending", StringComparison.Ordinal))
            {
                record.Status = "Pending Create Finish";

                _entries.Add(
                    new MonitorEntry
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        AppName = record.SourceApp,
                        Direction = "⚠ TIMEOUT",
                        Topic = "account.create",
                        CorrelationId = correlationId,
                        Summary = "No response from ERP after 10s — account creation pending",
                        CssClass = "topic-timeout"
                    }
                );

                NotifyChanged();
            }
        }
        catch (OperationCanceledException)
        {
            // account.created arrived in time — no action needed
        }
    }


    private async Task PublishAutoResponseAsync(
        string responseTopic,
        string respondingApp,
        string? correlationId,
        Dictionary<string, string> originalFields,
        string originalSourceApp)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);

        var merged = new Dictionary<string, string>(originalFields);
        foreach (var kv in GenerateDefaultFields(responseTopic))
            merged[kv.Key] = kv.Value;

        var responseCorrelationId = BuildResponseCorrelationId(
            responseTopic, respondingApp, originalSourceApp, correlationId, merged
        );

        Publish(responseTopic, respondingApp, responseCorrelationId, merged, isUserInitiated: false);
    }


    private static string? BuildResponseCorrelationId(
        string responseTopic,
        string respondingApp,
        string originalSourceApp,
        string? originalCorrelationId,
        Dictionary<string, string> mergedFields)
    {
        if (!string.Equals(responseTopic, "account.created", StringComparison.Ordinal))
            return originalCorrelationId;

        // ERP originated the account — use the minted account number
        if (string.Equals(originalSourceApp, respondingApp, StringComparison.OrdinalIgnoreCase) &&
            mergedFields.TryGetValue("CustomerId", out var custId) &&
            !string.IsNullOrEmpty(custId))
        {
            return custId;
        }

        // Another app originated it — pass through their correlation ID unchanged
        return originalCorrelationId;
    }


    private Dictionary<string, string> GenerateDefaultFields(string topic)
    {
        var topicConfig = _config.GetTopic(topic);
        if (topicConfig is null) return new Dictionary<string, string>();

        var now = DateTimeOffset.UtcNow;
        var result = new Dictionary<string, string>();

        foreach (var field in topicConfig.Fields)
            result[field.Name] = PlaceholderResolver.Resolve(field.Default, now, Random.Shared);

        return result;
    }


    private void InitializeAppStatus()
    {
        foreach (var app in _config.Apps)
        {
            _onlineStatus[app.Name] = app.DefaultOnline;
            _queued[app.Name] = new Queue<QueuedMessage>();
        }
    }


    private void NotifyChanged() => OnChanged?.Invoke();


    private static string? BuildReceiverSummary(
        string appName,
        string topic,
        Dictionary<string, string> fields,
        Dictionary<string, string>? oldFields = null)
    {
        if (string.Equals(topic, "account.created", StringComparison.Ordinal))
        {
            fields.TryGetValue("CustomerId", out var id);
            fields.TryGetValue("Name", out var name);

            if (string.Equals(appName, "CRM", StringComparison.Ordinal))
                return $"Account record updated — AccountNumber set to {id ?? "?"}";

            if (string.Equals(appName, "Data Warehouse", StringComparison.Ordinal))
                return $"Inserted new record — Account {id ?? "?"}: {name ?? "?"}";
        }

        if (string.Equals(topic, "account.deactivated", StringComparison.Ordinal))
        {
            fields.TryGetValue("CustomerId", out var id);
            fields.TryGetValue("Name", out var name);
            return $"Customer {id ?? "?"} ({name ?? "?"}) marked inactive";
        }

        if (string.Equals(topic, "account.activated", StringComparison.Ordinal))
        {
            fields.TryGetValue("CustomerId", out var id);
            fields.TryGetValue("Name", out var name);
            return $"Customer {id ?? "?"} ({name ?? "?"}) re-activated";
        }

        if (string.Equals(topic, "account.updated", StringComparison.Ordinal) && oldFields is not null)
        {
            var changes = new List<string>();

            foreach (var kv in fields.Where(kv => !string.Equals(kv.Key, "CustomerId", StringComparison.Ordinal)))
            {
                if (oldFields.TryGetValue(kv.Key, out var oldVal) &&
                    !string.Equals(oldVal, kv.Value, StringComparison.Ordinal))
                {
                    changes.Add($"{kv.Key}: \"{oldVal}\" → \"{kv.Value}\"");
                }
            }

            if (changes.Count > 0)
                return string.Join(", ", changes);
        }

        return null;
    }


    private static string BuildSummary(Dictionary<string, string> fields) =>
        string.Join(", ", fields.Select(kv => $"{kv.Key}: {kv.Value}"));
}
