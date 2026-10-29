namespace EsbDemo.Web.Models;

/// <summary>
/// The monitor's view of one customer account as it moves through the bus.
/// </summary>
/// <remarks>
/// A record starts as <c>Pending</c> when an app publishes <c>account.create</c>, becomes
/// <c>Active</c> when <c>account.created</c> arrives with a <see cref="CustomerId"/>, and becomes
/// <c>Pending Create Finish</c> if no reply arrives within 10 seconds.
/// </remarks>
public sealed class AccountRecord
{
    /// <summary>
    /// Gets the correlation ID of the message that started tracking the account; empty when the account was first seen in a reply or update.
    /// </summary>
    public required string CorrelationId { get; init; }



    /// <summary>
    /// Gets the name of the app that requested the account; empty when unknown.
    /// </summary>
    public required string SourceApp { get; init; }



    /// <summary>
    /// Gets or sets the customer number assigned by the ERP, or <see langword="null"/> while creation is pending.
    /// </summary>
    public string? CustomerId { get; set; }



    /// <summary>
    /// Gets or sets the account's lifecycle status: <c>Pending</c> (the default), <c>Active</c> or <c>Pending Create Finish</c>.
    /// </summary>
    public string Status { get; set; } = "Pending";



    /// <summary>
    /// Gets the latest value of each payload field seen for the account, keyed case-insensitively.
    /// </summary>
    public Dictionary<string, string> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
}
