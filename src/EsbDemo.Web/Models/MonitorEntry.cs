namespace EsbDemo.Web.Models;

/// <summary>
/// One row of the bus monitor's message log.
/// </summary>
public sealed record MonitorEntry
{
    /// <summary>
    /// Gets when the message was sent, in UTC. A queued delivery keeps its original send time.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }


    /// <summary>
    /// Gets the name of the app that sent or received the message.
    /// </summary>
    public string AppName { get; init; } = string.Empty;


    /// <summary>
    /// Gets the direction label: <c>→ SEND</c>, <c>← RECV</c> or <c>⚠ TIMEOUT</c>.
    /// </summary>
    public string Direction { get; init; } = string.Empty;


    /// <summary>
    /// Gets the message topic.
    /// </summary>
    public string Topic { get; init; } = string.Empty;


    /// <summary>
    /// Gets the message's correlation ID, or <see langword="null"/> if it has none.
    /// </summary>
    public string? CorrelationId { get; init; }


    /// <summary>
    /// Gets the human-readable description of the message shown in the log.
    /// </summary>
    public string Summary { get; init; } = string.Empty;


    /// <summary>
    /// Gets the CSS class that colours the row, usually from <see cref="Services.ConfigService.GetTopicCssClass"/>.
    /// </summary>
    public string CssClass { get; init; } = string.Empty;


    /// <summary>
    /// Gets a value indicating whether the message waited in an offline app's queue and was delivered
    /// when the app came back online. The monitor styles these rows as queued deliveries.
    /// </summary>
    public bool IsQueued { get; init; }
}
