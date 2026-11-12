using EsbDemo.Web.Data.DataWarehouse;
using EsbDemo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Connectors;

/// <summary>
/// Receive-only connector for the Data Warehouse.
/// Listens for ESB account events and upserts <see cref="DwCustomer"/> records.
/// </summary>
public sealed class DataWarehouseConnector
{
    private readonly IDbContextFactory<DataWarehouseDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;


    /// <summary>Fired after an inbound ESB message updates the Data Warehouse database.</summary>
    public event EventHandler? DataChanged;


    /// <summary>
    /// Initializes a new instance of the <see cref="DataWarehouseConnector"/> class and registers its inbound handler
    /// with <paramref name="monitor"/> under the app name <c>Data Warehouse</c>.
    /// </summary>
    /// <param name="monitor">The bus monitor that delivers messages to the Data Warehouse system.</param>
    /// <param name="dbFactory">Creates the short-lived context each inbound message uses to reach the Data Warehouse database.</param>
    /// <param name="timeProvider">The clock that stamps the rows an inbound message creates or changes.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="monitor"/>, <paramref name="dbFactory"/> or <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    public DataWarehouseConnector(MonitorService monitor, IDbContextFactory<DataWarehouseDbContext> dbFactory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(dbFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _dbFactory = dbFactory;
        _timeProvider = timeProvider;
        monitor.RegisterHandler("Data Warehouse", HandleInboundAsync);
    }


    private async Task HandleInboundAsync(string topic, Dictionary<string, string> fields)
    {
        if (!fields.TryGetValue("CustomerId", out var custNum) || string.IsNullOrEmpty(custNum))
            return;

        var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using var dbDisposal = db.ConfigureAwait(false);

        var row = await db.Customers.FirstOrDefaultAsync(
            c => c.CustNum == custNum
        ).ConfigureAwait(false);

        if (string.Equals(topic, "account.created", StringComparison.Ordinal))
        {
            if (row is null)
                await AddCustomerAsync(db, custNum, fields).ConfigureAwait(false);
        }
        else if (string.Equals(topic, "account.updated", StringComparison.Ordinal))
        {
            if (row is null) return;
            if (fields.TryGetValue("Name",  out var name))  row.Name  = name;
            if (fields.TryGetValue("Email", out var email)) row.Email = email;
            if (fields.TryGetValue("Phone", out var phone)) row.Phone = phone;
            row.LastUpdated = _timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (string.Equals(topic, "account.deactivated", StringComparison.Ordinal))
        {
            if (row is null) return;
            row.IsActive    = false;
            row.LastUpdated = _timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (string.Equals(topic, "account.activated", StringComparison.Ordinal))
        {
            if (row is null) return;
            row.IsActive    = true;
            row.LastUpdated = _timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    /// <summary>Adds a <see cref="DwCustomer"/> for a customer the warehouse has not seen before.</summary>
    private async Task AddCustomerAsync(DataWarehouseDbContext db, string custNum, Dictionary<string, string> fields)
    {
        fields.TryGetValue("Name",  out var name);
        fields.TryGetValue("Email", out var email);
        fields.TryGetValue("Phone", out var phone);

        var row = new DwCustomer
        {
            CustNum       = custNum,
            Name          = name ?? custNum,
            Email         = email,
            Phone         = phone,
            IsActive      = true,
            FirstSeenDate = _timeProvider.GetUtcNow().UtcDateTime,
            LastUpdated   = _timeProvider.GetUtcNow().UtcDateTime
        };
        await db.Customers.AddAsync(row).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
