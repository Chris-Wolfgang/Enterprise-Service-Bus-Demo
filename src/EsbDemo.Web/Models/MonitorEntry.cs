namespace EsbDemo.Web.Models;

public sealed record MonitorEntry
{
    public DateTimeOffset Timestamp { get; init; }


    public string AppName { get; init; } = string.Empty;


    public string Direction { get; init; } = string.Empty;


    public string Topic { get; init; } = string.Empty;


    public string? CorrelationId { get; init; }


    public string Summary { get; init; } = string.Empty;


    public string CssClass { get; init; } = string.Empty;


    public bool IsQueued { get; init; }
}
