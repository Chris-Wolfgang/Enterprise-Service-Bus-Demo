namespace EsbDemo.Web.Models;

internal sealed class QueuedMessage
{
    public required string Topic { get; init; }



    public required string SourceApp { get; init; }



    public string? CorrelationId { get; init; }



    public required Dictionary<string, string> Fields { get; init; }



    public bool IsUserInitiated { get; init; }



    public DateTimeOffset Timestamp { get; init; }
}
