using EsbDemo.Core.Config;
using EsbDemo.Core.Services;
using EsbDemo.Web.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EsbDemo.Web.Services;

/// <summary>
/// The mock in-memory bus: routes published messages to subscribing apps, simulates apps going
/// offline, and keeps the message log and account tracking that the UI displays.
/// </summary>
/// <remarks>
/// The service is a singleton shared by every circuit and is safe to call from any thread.
/// <see cref="Entries"/> and <see cref="Accounts"/> return snapshots. Connector handlers run
/// after the bus lock is released, and a handler, auto-response or timeout task that faults is
/// logged rather than lost.
/// </remarks>
public sealed partial class MonitorService
{
    /// <summary>How long an app takes to send its configured auto-response.</summary>
    internal static readonly TimeSpan AutoResponseDelay = TimeSpan.FromMilliseconds(500);



    /// <summary>How long an <c>account.create</c> waits for the matching <c>account.created</c>.</summary>
    internal static readonly TimeSpan AccountCreateTimeout = TimeSpan.FromSeconds(10);



    private readonly ConfigService _config;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MonitorService> _logger;

    // Guards every collection below. Blazor circuits call in on their own threads, and the delayed
    // auto-response and timeout tasks call in from the thread pool, so all access is serialized.
    private readonly Lock _gate = new();

    // Backs OnChanged. Explicit accessors because NotifyChanged raises each subscriber separately
    // through the invocation list rather than through a single field-like event invoke.
    private EventHandler? _onChanged;

    /// <summary>The most entries the log keeps; older ones are dropped first.</summary>
    internal const int MaxEntries = 500;

    private readonly List<MonitorEntry> _entries = new();
    private readonly Dictionary<string, bool> _onlineStatus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<QueuedMessage>> _queued = new(StringComparer.OrdinalIgnoreCase);

    // Connector handlers — keyed by app name, called when a message is delivered to that app
    private readonly Dictionary<string, Func<string, Dictionary<string, string>, Task>> _handlers
        = new(StringComparer.OrdinalIgnoreCase);

    // Account tracking
    private readonly Dictionary<string, AccountRecord> _accountsByCorrelation = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AccountRecord> _accountsById = new(StringComparer.OrdinalIgnoreCase);

    // Customers that already exist in the systems' databases (the seeded ones). SetBus keeps them,
    // because switching the bus does not reset the databases.
    private readonly Dictionary<string, AccountRecord> _existingAccounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _pendingCreateCts = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the name of the bus implementation in use. Defaults to <c>Mock</c>.
    /// </summary>
    public string SelectedBus { get; private set; } = "Mock";


    /// <summary>
    /// Raised after the log, account tracking or app status changes.
    /// </summary>
    /// <remarks>
    /// It can be raised from a thread-pool thread, so UI subscribers should marshal to their
    /// renderer, for example with <c>InvokeAsync</c>.
    /// </remarks>
    public event EventHandler? OnChanged
    {
        add
        {
            lock (_gate)
                _onChanged += value;
        }
        remove
        {
            lock (_gate)
                _onChanged -= value;
        }
    }


    /// <summary>
    /// Gets a snapshot of the message log, oldest first.
    /// </summary>
    public IReadOnlyList<MonitorEntry> Entries
    {
        get
        {
            lock (_gate)
                return [.. _entries];
        }
    }


    /// <summary>
    /// Gets a snapshot of the accounts tracked by correlation ID. The records are copies, so later
    /// bus activity does not change them.
    /// </summary>
    public IReadOnlyList<AccountRecord> Accounts
    {
        get
        {
            lock (_gate)
                return [.. _accountsByCorrelation.Values.Select(CopyOf)];
        }
    }


    /// <summary>
    /// Gets a snapshot of every customer the app panel can target: the accounts tracked through the
    /// bus (including pending <c>account.create</c> requests and customers first seen in an
    /// <c>account.updated</c>) and the customers registered with <see cref="TrackExistingAccount"/>.
    /// </summary>
    /// <remarks>
    /// Each customer number appears once, with the most recent field values the bus has seen for it.
    /// Customers with a number come first, ordered by number; pending requests follow.
    /// The records are copies, so later bus activity does not change them.
    /// </remarks>
    public IReadOnlyList<AccountRecord> KnownAccounts
    {
        get
        {
            lock (_gate)
            {
                var byNumber = new Dictionary<string, AccountRecord>(StringComparer.OrdinalIgnoreCase);
                var pending = new List<AccountRecord>();

                foreach (var record in _accountsByCorrelation.Values)
                {
                    if (record.CustomerId is null)
                        pending.Add(CopyOf(record));
                    else
                        byNumber.TryAdd(record.CustomerId, CopyOf(record));
                }

                foreach (var (id, record) in _accountsById)
                    byNumber.TryAdd(id, CopyOf(record));

                foreach (var (id, record) in _existingAccounts)
                    byNumber.TryAdd(id, CopyOf(record));

                return
                [
                    .. byNumber.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).Select(kv => kv.Value),
                    .. pending
                ];
            }
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorService"/> class and sets every configured
    /// app to its <see cref="AppConfig.DefaultOnline"/> status.
    /// </summary>
    /// <param name="config">The app and topic configuration, which must already be loaded.</param>
    public MonitorService(ConfigService config)
        : this(config, TimeProvider.System, NullLogger<MonitorService>.Instance)
    {
    }



    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorService"/> class that logs faults from
    /// connector handlers and background tasks, and sets every configured app to its
    /// <see cref="AppConfig.DefaultOnline"/> status.
    /// </summary>
    /// <param name="config">The app and topic configuration, which must already be loaded.</param>
    /// <param name="logger">Receives faults from connector handlers, auto-responses and timeouts.</param>
    public MonitorService(ConfigService config, ILogger<MonitorService> logger)
        : this(config, TimeProvider.System, logger)
    {
    }



    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorService"/> class with the clock that
    /// timestamps entries and drives the auto-response and account-creation delays. Tests pass a
    /// fake clock so those delays don't need real waiting.
    /// </summary>
    /// <param name="config">The app and topic configuration, which must already be loaded.</param>
    /// <param name="timeProvider">The clock to use.</param>
    internal MonitorService(ConfigService config, TimeProvider timeProvider)
        : this(config, timeProvider, NullLogger<MonitorService>.Instance)
    {
    }



    /// <summary>
    /// Initializes a new instance of the <see cref="MonitorService"/> class with a clock and a logger.
    /// </summary>
    /// <param name="config">The app and topic configuration, which must already be loaded.</param>
    /// <param name="timeProvider">The clock to use.</param>
    /// <param name="logger">Receives faults from connector handlers, auto-responses and timeouts.</param>
    internal MonitorService(ConfigService config, TimeProvider timeProvider, ILogger<MonitorService> logger)
    {
        _config = config;
        _timeProvider = timeProvider;
        _logger = logger;
        InitializeAppStatus();
    }


    /// <summary>
    /// Registers a connector handler that is called whenever a message is delivered to
    /// <paramref name="appName"/>. The handler is NOT called when the app itself is the source,
    /// preventing loops.
    /// </summary>
    /// <remarks>
    /// The handler is also called with the app's own auto-response, and awaited, before that reply
    /// is published, so the responding system stores what it announces. For example, the ERP saves
    /// the customer it mints for <c>account.created</c> before the other systems receive it.
    /// </remarks>
    /// <param name="appName">The app name, matched case-insensitively.</param>
    /// <param name="handler">Receives the topic and a copy of the payload.</param>
    public void RegisterHandler(string appName, Func<string, Dictionary<string, string>, Task> handler)
    {
        lock (_gate)
            _handlers[appName] = handler;
    }


    /// <summary>
    /// Gets a value indicating whether an app is online.
    /// </summary>
    /// <param name="appName">The app name, matched case-insensitively.</param>
    /// <returns><see langword="true"/> if the app is online or unknown; otherwise <see langword="false"/>.</returns>
    public bool IsOnline(string appName)
    {
        lock (_gate)
            return IsOnlineCore(appName);
    }


    /// <summary>
    /// Gets the number of messages waiting for an offline app.
    /// </summary>
    /// <param name="appName">The app name, matched case-insensitively.</param>
    /// <returns>The number of queued messages; 0 for an unknown app.</returns>
    public int GetQueuedCount(string appName)
    {
        lock (_gate)
            return _queued.TryGetValue(appName, out var q) ? q.Count : 0;
    }


    /// <summary>
    /// Takes an app online or offline. Messages for an offline app are queued; bringing it back
    /// online delivers them in the order they were sent.
    /// </summary>
    /// <param name="appName">The app name, matched case-insensitively.</param>
    /// <param name="online"><see langword="true"/> to take the app online; <see langword="false"/> to take it offline.</param>
    public void SetOnline(string appName, bool online)
    {
        var handlerCalls = new List<HandlerCall>();

        lock (_gate)
        {
            _onlineStatus[appName] = online;

            if (online && _queued.TryGetValue(appName, out var queue))
            {
                var app = _config.Apps.FirstOrDefault(
                    a => string.Equals(a.Name, appName, StringComparison.OrdinalIgnoreCase)
                );

                while (queue.TryDequeue(out var msg))
                {
                    FlushQueuedMessage(app, appName, msg, handlerCalls);
                }
            }
        }

        InvokeHandlers(handlerCalls);
        NotifyChanged();
    }


    /// <summary>
    /// Registers a customer that already exists in the systems' databases, such as a seeded one, so
    /// <see cref="KnownAccounts"/> offers it before any message about it has crossed the bus.
    /// </summary>
    /// <remarks>
    /// Registering the same customer number again replaces its fields. <see cref="SetBus"/> keeps
    /// these registrations.
    /// </remarks>
    /// <param name="customerId">The shared customer number, such as <c>CUST0001</c>.</param>
    /// <param name="fields">The customer's canonical fields (for example <c>Name</c>, <c>Email</c>, <c>Phone</c>).</param>
    /// <exception cref="ArgumentException"><paramref name="customerId"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="customerId"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    public void TrackExistingAccount(string customerId, IReadOnlyDictionary<string, string> fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        ArgumentNullException.ThrowIfNull(fields);

        var record = new AccountRecord
        {
            CorrelationId = string.Empty,
            SourceApp = string.Empty,
            CustomerId = customerId,
            Status = "Active"
        };

        foreach (var kv in fields)
            record.Fields[kv.Key] = kv.Value;

        lock (_gate)
            _existingAccounts[customerId] = record;

        NotifyChanged();
    }


    /// <summary>
    /// Switches the bus implementation and resets the log, account tracking, pending timeouts and
    /// queued messages.
    /// </summary>
    /// <param name="bus">The name of the bus to use.</param>
    public void SetBus(string bus)
    {
        lock (_gate)
        {
            SelectedBus = bus;
            _entries.Clear();

            foreach (var cts in _pendingCreateCts.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _pendingCreateCts.Clear();
            _accountsByCorrelation.Clear();
            _accountsById.Clear();

            foreach (var q in _queued.Values)
                q.Clear();
        }

        NotifyChanged();
    }


    /// <summary>
    /// Publishes a message to every app that subscribes to <paramref name="topic"/>.
    /// </summary>
    /// <remarks>
    /// Each online subscriber logs the message and passes it to its connector; an offline subscriber
    /// queues it. The source app logs its own copy without acting on it. An <c>account.create</c>
    /// starts a 10-second wait for the matching <c>account.created</c>.
    /// </remarks>
    /// <param name="topic">The topic to publish.</param>
    /// <param name="sourceApp">The name of the publishing app.</param>
    /// <param name="correlationId">The correlation ID that links a request to its replies, or <see langword="null"/>.</param>
    /// <param name="fields">The payload, keyed by field name. Each subscriber receives its own copy.</param>
    /// <param name="isUserInitiated">
    /// <see langword="true"/> when a person triggered the message. Only user-initiated messages fire
    /// <see cref="AppConfig.AutoResponses"/>, which keeps replies from triggering further replies.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <see langword="null"/>.</exception>
    public void Publish(
        string topic,
        string sourceApp,
        string? correlationId,
        Dictionary<string, string> fields,
        bool isUserInitiated = false
    )
    {
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        var handlerCalls = new List<HandlerCall>();

        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();

            TrackAccountCreate(topic, sourceApp, correlationId, fields);

            var oldAccountFields = CaptureOldAccountFields(topic, fields);

            var summary = BuildSummary(fields);
            var cssClass = _config.GetTopicCssClass(topic);

            AddEntry(
                new MonitorEntry
                {
                    Timestamp = now,
                    AppName = sourceApp,
                    Direction = MonitorDirection.Send,
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
                    summary, now, isUserInitiated, handlerCalls, oldAccountFields
                );
            }
        }

        InvokeHandlers(handlerCalls);
        NotifyChanged();
    }


    /// <summary>
    /// Applies a message to <paramref name="sourceApp"/>'s own data through its connector, then
    /// publishes it. Use it for a message composed for a system outside that system's own UI (the
    /// app panel), so the sender ends up with the data it announces, as every subscriber does.
    /// </summary>
    /// <remarks>
    /// This mirrors how an auto-response is stored by the responding system before it is published:
    /// for example, a <c>Customer Created</c> sent from the ERP panel adds the customer to the ERP
    /// customer master. A fault in the source's handler is logged, and the message is still published.
    /// The tabs do not need this: they write their own database and then publish.
    /// </remarks>
    /// <param name="topic">The topic to publish.</param>
    /// <param name="sourceApp">The name of the publishing app.</param>
    /// <param name="correlationId">The correlation ID that links a request to its replies, or <see langword="null"/>.</param>
    /// <param name="fields">The payload, keyed by field name. The source and each subscriber receive their own copy.</param>
    /// <param name="isUserInitiated">
    /// <see langword="true"/> when a person triggered the message; see <see cref="Publish"/>.
    /// </param>
    /// <returns>A task that completes once the message is applied to the source and published.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <see langword="null"/>.</exception>
    public async Task PublishAndApplyAsync(
        string topic,
        string sourceApp,
        string? correlationId,
        Dictionary<string, string> fields,
        bool isUserInitiated = false
    )
    {
        ArgumentNullException.ThrowIfNull(fields);

        Func<string, Dictionary<string, string>, Task>? sourceHandler;
        lock (_gate)
            _handlers.TryGetValue(sourceApp, out sourceHandler);

        if (sourceHandler is not null)
        {
            await ObserveAsync
            (
                () => sourceHandler(topic, new Dictionary<string, string>(fields)),
                $"{sourceApp} handler for its own {topic}"
            ).ConfigureAwait(false);
        }

        Publish(topic, sourceApp, correlationId, fields, isUserInitiated);
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

        // A repeated correlation ID replaces the earlier wait; cancel it so it cannot fire for the new record.
        if (_pendingCreateCts.Remove(correlationId, out var previous))
        {
            previous.Cancel();
            previous.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pendingCreateCts[correlationId] = cts;
        var token = cts.Token;
        Observe(() => WatchAccountCreateTimeoutAsync(correlationId, token), "account.create timeout");
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
            cts.Dispose();
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
        DateTimeOffset now,
        bool isUserInitiated,
        List<HandlerCall> handlerCalls,
        Dictionary<string, string>? oldAccountFields = null
    )
    {
        if (IsOnlineCore(app.Name))
        {
            Receive(
                app, app.Name, topic, sourceApp, correlationId, fields, sendSummary,
                now, isUserInitiated, oldAccountFields, isQueued: false, handlerCalls
            );
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
                Timestamp = now,
                OldAccountFields = oldAccountFields is null ? null : new Dictionary<string, string>(oldAccountFields)
            });
        }
    }


    private void FlushQueuedMessage(AppConfig? app, string appName, QueuedMessage msg, List<HandlerCall> handlerCalls)
    {
        Receive(
            app, appName, msg.Topic, msg.SourceApp, msg.CorrelationId, msg.Fields, sendSummary: null,
            msg.Timestamp, msg.IsUserInitiated, msg.OldAccountFields, isQueued: true, handlerCalls
        );
    }


    // Caller holds _gate. The one place that logs a delivery and decides its side effects, so a
    // message delivered from an offline system's queue reads and acts exactly as a live one: the
    // sender's own copy is marked self-originated and fires nothing, an account.updated keeps its
    // old-to-new diff, and only other systems' copies trigger auto-responses and connector handlers.
    private void Receive(
        AppConfig? app,
        string appName,
        string topic,
        string sourceApp,
        string? correlationId,
        Dictionary<string, string> fields,
        string? sendSummary,
        DateTimeOffset timestamp,
        bool isUserInitiated,
        Dictionary<string, string>? oldAccountFields,
        bool isQueued,
        List<HandlerCall> handlerCalls
    )
    {
        var isSelfOrigin = string.Equals(appName, sourceApp, StringComparison.OrdinalIgnoreCase);

        AddEntry(
            new MonitorEntry
            {
                Timestamp = timestamp,
                AppName = appName,
                Direction = MonitorDirection.Receive,
                Topic = topic,
                CorrelationId = correlationId,
                Summary = isSelfOrigin
                    ? "Self-originated — no action taken"
                    : BuildReceiverSummary(appName, topic, fields, oldAccountFields) ?? sendSummary ?? BuildSummary(fields),
                CssClass = isSelfOrigin ? "topic-self" : _config.GetTopicCssClass(topic),
                IsQueued = isQueued
            }
        );

        if (isSelfOrigin)
            return;

        if (app is not null)
            TriggerAutoResponse(app, topic, correlationId, fields, isUserInitiated);

        if (_handlers.TryGetValue(appName, out var handler))
            handlerCalls.Add(new HandlerCall(appName, handler, topic, new Dictionary<string, string>(fields)));
    }


    private void TriggerAutoResponse(
        AppConfig app,
        string topic,
        string? correlationId,
        Dictionary<string, string> fields,
        bool isUserInitiated
    )
    {
        if (!isUserInitiated) return;

        var autoResponse = app.AutoResponses.FirstOrDefault(
            r => string.Equals(r.On, topic, StringComparison.Ordinal)
        );

        if (autoResponse is not null &&
            !string.Equals(autoResponse.Publish, topic, StringComparison.Ordinal))
        {
            Observe
            (
                () => PublishAutoResponseAsync(autoResponse.Publish, app.Name, correlationId, fields),
                $"{app.Name} auto-response {autoResponse.Publish}"
            );
        }
    }


    private async Task WatchAccountCreateTimeoutAsync(string correlationId, CancellationToken ct)
    {
        try
        {
            await Task.Delay(AccountCreateTimeout, _timeProvider, ct).ConfigureAwait(false);

            var timedOut = false;
            lock (_gate)
            {
                if (_accountsByCorrelation.TryGetValue(correlationId, out var record) &&
                    string.Equals(record.Status, "Pending", StringComparison.Ordinal))
                {
                    record.Status = "Pending Create Finish";

                    AddEntry(
                        new MonitorEntry
                        {
                            Timestamp = _timeProvider.GetUtcNow(),
                            AppName = record.SourceApp,
                            Direction = MonitorDirection.Timeout,
                            Topic = "account.create",
                            CorrelationId = correlationId,
                            Summary = "No response from ERP after 10s — account creation pending",
                            CssClass = "topic-timeout"
                        }
                    );

                    timedOut = true;
                }
            }

            if (timedOut)
                NotifyChanged();
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
        Dictionary<string, string> originalFields
    )
    {
        await Task.Delay(AutoResponseDelay, _timeProvider, CancellationToken.None).ConfigureAwait(false);

        // The reply describes the same customer or order as the request, so the request's values
        // win. Generated defaults only fill fields the request did not carry or left empty, such
        // as the CustomerId the ERP mints for account.created.
        var merged = new Dictionary<string, string>(originalFields);
        foreach (var kv in GenerateDefaultFields(responseTopic))
        {
            if (!merged.TryGetValue(kv.Key, out var existing) || string.IsNullOrEmpty(existing))
                merged[kv.Key] = kv.Value;
        }

        // Let the responding system store what it is about to announce (the ERP saves the customer
        // it minted) before any other system receives the reply.
        Func<string, Dictionary<string, string>, Task>? responderHandler;
        lock (_gate)
            _handlers.TryGetValue(respondingApp, out responderHandler);

        if (responderHandler is not null)
            await responderHandler(responseTopic, new Dictionary<string, string>(merged)).ConfigureAwait(false);

        // A reply carries the request's correlation ID. An app never answers its own request (a
        // self-originated copy fires no auto-response), so the reply always links back to another
        // app's request.
        Publish(responseTopic, respondingApp, correlationId, merged, isUserInitiated: false);
    }


    private Dictionary<string, string> GenerateDefaultFields(string topic)
    {
        var topicConfig = _config.GetTopic(topic);
        if (topicConfig is null) return new Dictionary<string, string>();

        var now = _timeProvider.GetUtcNow();
        var result = new Dictionary<string, string>();

        foreach (var field in topicConfig.Fields)
            result[field.Name] = PlaceholderResolver.Resolve(field.Default, now, Random.Shared);

        return result;
    }


    // Called after the bus lock is released, so a connector's synchronous work (context creation, the
    // first EF call) never runs under the lock, and one handler's fault cannot stop the others.
    private void InvokeHandlers(List<HandlerCall> handlerCalls)
    {
        foreach (var call in handlerCalls)
            Observe(() => call.Handler(call.Topic, call.Fields), $"{call.AppName} handler for {call.Topic}");
    }



    // Caller holds _gate. Keeps the singleton log bounded for a long-running demo.
    private void AddEntry(MonitorEntry entry)
    {
        _entries.Add(entry);

        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(0, _entries.Count - MaxEntries);
    }



    // Starts the work inside the try, so a synchronous throw from it is logged the same way as a
    // faulted task, and the awaited task is always one this method started.
    private void Observe(Func<Task> start, string operation) =>
        _ = ObserveAsync(start, operation);



    private async Task ObserveAsync(Func<Task> start, string operation)
    {
        try
        {
            await start().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogBackgroundFault(_logger, ex, operation);
        }
    }



    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Bus background work failed: {Operation}")]
    private static partial void LogBackgroundFault(ILogger logger, Exception exception, string operation);



    private void InitializeAppStatus()
    {
        foreach (var app in _config.Apps)
        {
            _onlineStatus[app.Name] = app.DefaultOnline;
            _queued[app.Name] = new Queue<QueuedMessage>();
        }
    }


    private static AccountRecord CopyOf(AccountRecord source)
    {
        var copy = new AccountRecord
        {
            CorrelationId = source.CorrelationId,
            SourceApp = source.SourceApp,
            CustomerId = source.CustomerId,
            Status = source.Status
        };

        foreach (var kv in source.Fields)
            copy.Fields[kv.Key] = kv.Value;

        return copy;
    }


    private bool IsOnlineCore(string appName) =>
        !_onlineStatus.TryGetValue(appName, out var online) || online;


    // Invokes each subscriber on its own, so one throwing UI subscriber neither stops the others
    // nor propagates into the Publish / SetOnline / SetBus call that raised the event (#165).
    private void NotifyChanged()
    {
        if (Volatile.Read(ref _onChanged) is not { } handlers)
            return;

        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler>())
        {
            try
            {
                handler(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogSubscriberFault(_logger, ex);
            }
        }
    }



    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "An OnChanged subscriber threw")]
    private static partial void LogSubscriberFault(ILogger logger, Exception exception);


    private static string? BuildReceiverSummary(
        string appName,
        string topic,
        Dictionary<string, string> fields,
        Dictionary<string, string>? oldFields = null
    )
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



    private readonly record struct HandlerCall
    (
        string AppName,
        Func<string, Dictionary<string, string>, Task> Handler,
        string Topic,
        Dictionary<string, string> Fields
    );
}
