namespace EsbDemo.Web.Models;

public sealed class AccountRecord
{
    public required string CorrelationId { get; init; }


    public required string SourceApp { get; init; }


    public string? CustomerId { get; set; }


    public string Status { get; set; } = "Pending";


    public Dictionary<string, string> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
}
