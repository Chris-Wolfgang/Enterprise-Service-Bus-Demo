using EsbDemo.Web.Data.CustomerPortal;
using EsbDemo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Connectors;

/// <summary>
/// Translates between the Customer Portal's data model and the ESB's canonical message format.
/// Inbound: upserts <see cref="PortalCustomer"/> records from account events.
/// Outbound: publishes <c>account.updated</c> when a customer edits their own profile.
/// </summary>
public sealed class CustomerPortalConnector
{
    private readonly MonitorService _monitor;
    private readonly IServiceScopeFactory _scopeFactory;


    /// <summary>Fired after an inbound ESB message updates the Customer Portal database.</summary>
    public event Action? DataChanged;


    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerPortalConnector"/> class and registers its inbound handler
    /// with <paramref name="monitor"/> under the app name <c>Customer Portal</c>.
    /// </summary>
    /// <param name="monitor">The bus monitor that delivers messages to the Customer Portal system.</param>
    /// <param name="scopeFactory">Creates the scope each inbound message uses to reach the Customer Portal database.</param>
    /// <exception cref="ArgumentNullException"><paramref name="monitor"/> is <see langword="null"/>.</exception>
    public CustomerPortalConnector(MonitorService monitor, IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        _monitor = monitor;
        _scopeFactory = scopeFactory;
        monitor.RegisterHandler("Customer Portal", HandleInboundAsync);
    }


    // ── Outbound: Customer Portal → ESB ──────────────────────────────────

    /// <summary>Translates a portal profile edit to the ESB <c>account.updated</c> event.</summary>
    public void PublishAccountUpdated(PortalCustomer cust)
    {
        ArgumentNullException.ThrowIfNull(cust);
        _monitor.Publish(
            "account.updated", "Customer Portal",
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


    // ── Inbound: ESB → Customer Portal ───────────────────────────────────

    private async Task HandleInboundAsync(string topic, Dictionary<string, string> fields)
    {
        if (!fields.TryGetValue("CustomerId", out var custNum) || string.IsNullOrEmpty(custNum))
            return;

        var scope = _scopeFactory.CreateAsyncScope();
        await using var scopeDisposal = scope.ConfigureAwait(false);
        var db = scope.ServiceProvider.GetRequiredService<CustomerPortalDbContext>();

        var cust = await db.Customers.FirstOrDefaultAsync(
            c => c.CustNum == custNum
        ).ConfigureAwait(false);

        if (cust is null)
        {
            if (string.Equals(topic, "account.created", StringComparison.Ordinal))
            {
                fields.TryGetValue("Name",  out var name);
                fields.TryGetValue("Email", out var email);
                fields.TryGetValue("Phone", out var phone);

                await db.Customers.AddAsync(new PortalCustomer
                {
                    CustNum     = custNum,
                    Name        = name ?? custNum,
                    Email       = email,
                    Phone       = phone,
                    IsActive    = true,
                    CreatedDate = DateTime.UtcNow
                }).ConfigureAwait(false);
                await db.SaveChangesAsync().ConfigureAwait(false);
                DataChanged?.Invoke();
            }
            return;
        }

        if (string.Equals(topic, "account.updated", StringComparison.Ordinal))
        {
            if (fields.TryGetValue("Name",  out var name))  cust.Name  = name;
            if (fields.TryGetValue("Email", out var email)) cust.Email = email;
            if (fields.TryGetValue("Phone", out var phone)) cust.Phone = phone;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke();
        }
        else if (string.Equals(topic, "account.deactivated", StringComparison.Ordinal))
        {
            cust.IsActive = false;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke();
        }
        else if (string.Equals(topic, "account.activated", StringComparison.Ordinal))
        {
            cust.IsActive = true;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke();
        }
    }
}
