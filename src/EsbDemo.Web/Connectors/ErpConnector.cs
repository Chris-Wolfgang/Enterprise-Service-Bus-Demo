using EsbDemo.Web.Data.Erp;
using EsbDemo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Connectors;

/// <summary>
/// Translates between the ERP's internal data model and the ESB's canonical message format.
/// Outbound: maps ERP fields (CustNum, ClosedDate semantics) → ESB fields (CustomerId, status events).
/// Inbound:  maps ESB fields → ERP fields (Email→Email, account.deactivated→ClosedDate, etc.)
///           and updates the ERP database.
/// </summary>
public sealed class ErpConnector
{
    private readonly MonitorService _monitor;
    private readonly IServiceScopeFactory _scopeFactory;


    /// <summary>Fired after an inbound ESB message updates the ERP database.</summary>
    public event EventHandler? DataChanged;


    /// <summary>
    /// Initializes a new instance of the <see cref="ErpConnector"/> class and registers its inbound handler
    /// with <paramref name="monitor"/> under the app name <c>ERP</c>.
    /// </summary>
    /// <param name="monitor">The bus monitor that delivers messages to the ERP system.</param>
    /// <param name="scopeFactory">Creates the scope each inbound message uses to reach the ERP database.</param>
    /// <exception cref="ArgumentNullException"><paramref name="monitor"/> is <see langword="null"/>.</exception>
    public ErpConnector(MonitorService monitor, IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        _monitor = monitor;
        _scopeFactory = scopeFactory;
        monitor.RegisterHandler("ERP", HandleInboundAsync);
    }


    // ── Outbound: ERP → ESB ──────────────────────────────────────────────

    /// <summary>Translates an ERP customer update to the ESB <c>account.updated</c> event.</summary>
    public void PublishAccountUpdated(CustMast cust)
    {
        ArgumentNullException.ThrowIfNull(cust);
        _monitor.Publish(
            "account.updated", "ERP",
            correlationId: cust.CustNum,
            fields: new Dictionary<string, string>
            {
                // ERP CustNum is their customer identifier → ESB CustomerId
                ["CustomerId"] = cust.CustNum,
                ["Name"]       = cust.Name,
                ["Email"]      = cust.Email ?? "",
                ["Phone"]      = cust.Phone ?? ""
            },
            isUserInitiated: true
        );
    }

    /// <summary>
    /// Translates an ERP status change to <c>account.deactivated</c> or <c>account.activated</c>.
    /// ERP stores status as ClosedDate (null = active, non-null = inactive);
    /// the ESB uses separate named events.
    /// </summary>
    /// <param name="cust">The customer record after the status change was applied.</param>
    /// <param name="wasActive">Whether the account was active before the change.</param>
    public void PublishStatusChanged(CustMast cust, bool wasActive)
    {
        ArgumentNullException.ThrowIfNull(cust);

        // wasActive=true  → just set ClosedDate → publish account.deactivated
        // wasActive=false → just cleared ClosedDate → publish account.activated
        var topic = wasActive ? "account.deactivated" : "account.activated";

        _monitor.Publish(
            topic, "ERP",
            correlationId: cust.CustNum,
            fields: new Dictionary<string, string>
            {
                ["CustomerId"] = cust.CustNum,
                ["Name"]       = cust.Name
            },
            isUserInitiated: true
        );
    }

    /// <summary>
    /// Publishes <c>account.created</c> when a customer is added directly in ERP.
    /// ERP already owns the record — it notifies other systems rather than requesting creation.
    /// </summary>
    public void PublishNew(CustMast cust)
    {
        ArgumentNullException.ThrowIfNull(cust);
        _monitor.Publish(
            "account.created", "ERP",
            correlationId: cust.CustNum,
            fields: new Dictionary<string, string>
            {
                ["CustomerId"] = cust.CustNum,
                ["Name"]       = cust.Name,
                ["Email"]      = cust.Email ?? "",
                ["Phone"]      = cust.Phone ?? ""
            },
            isUserInitiated: true
        );
    }


    // ── Inbound: ESB → ERP ───────────────────────────────────────────────

    private async Task HandleInboundAsync(string topic, Dictionary<string, string> fields)
    {
        if (!fields.TryGetValue("CustomerId", out var customerId) || string.IsNullOrEmpty(customerId))
            return;

        var scope = _scopeFactory.CreateAsyncScope();
        await using var scopeDisposal = scope.ConfigureAwait(false);
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        // ERP identifies customers by CustNum, which equals the ESB's CustomerId
        var cust = await db.CustMast.FirstOrDefaultAsync(
            c => c.CustNum == customerId
        ).ConfigureAwait(false);

        if (cust is null)
        {
            // Only reached from the ERP's own account.created auto-response: store the minted customer.
            if (string.Equals(topic, "account.created", StringComparison.Ordinal))
                await CreateFromEsbAsync(db, customerId, fields).ConfigureAwait(false);

            return;
        }

        if (string.Equals(topic, "account.updated", StringComparison.Ordinal))
        {
            // ESB canonical field names map directly to ERP field names here
            if (fields.TryGetValue("Name",  out var name))  cust.Name  = name;
            if (fields.TryGetValue("Email", out var email)) cust.Email = email;
            if (fields.TryGetValue("Phone", out var phone)) cust.Phone = phone;
            cust.ModDate = DateTime.UtcNow;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (string.Equals(topic, "account.deactivated", StringComparison.Ordinal))
        {
            // ESB account.deactivated → ERP sets ClosedDate (their way of marking inactive)
            cust.ClosedDate = DateTime.UtcNow;
            cust.ModDate    = DateTime.UtcNow;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (string.Equals(topic, "account.activated", StringComparison.Ordinal))
        {
            // ESB account.activated → ERP clears ClosedDate (their way of marking active)
            cust.ClosedDate = null;
            cust.ModDate    = DateTime.UtcNow;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Adds the customer the ERP minted for an <c>account.created</c> reply.</summary>
    private async Task CreateFromEsbAsync(ErpDbContext db, string customerId, Dictionary<string, string> fields)
    {
        fields.TryGetValue("Name",  out var name);
        fields.TryGetValue("Email", out var email);
        fields.TryGetValue("Phone", out var phone);

        var cust = new CustMast
        {
            CustNum = customerId,
            Name    = name ?? customerId,
            Email   = email,
            Phone   = phone,
            AddDate = DateTime.UtcNow,
            ModDate = DateTime.UtcNow
        };
        await db.CustMast.AddAsync(cust).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
