using EsbDemo.Web.Data.Crm;
using EsbDemo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Connectors;

/// <summary>
/// Translates between CRM's internal data model and the ESB's canonical message format.
/// Outbound: maps CRM field names (EMailAddress1, Telephone1, StatusCode) → ESB field names (Email, Phone, status events).
/// Inbound:  maps ESB field names → CRM fields and updates the CRM database.
/// </summary>
public sealed class CrmConnector
{
    private readonly MonitorService _monitor;
    private readonly IDbContextFactory<CrmDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;


    /// <summary>Fired after an inbound ESB message updates the CRM database.</summary>
    public event EventHandler? DataChanged;


    /// <summary>
    /// Initializes a new instance of the <see cref="CrmConnector"/> class and registers its inbound handler
    /// with <paramref name="monitor"/> under the app name <c>CRM</c>.
    /// </summary>
    /// <param name="monitor">The bus monitor that delivers messages to the CRM system.</param>
    /// <param name="dbFactory">Creates the short-lived context each inbound message uses to reach the CRM database.</param>
    /// <param name="timeProvider">The clock that stamps the rows an inbound message creates or changes.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="monitor"/>, <paramref name="dbFactory"/> or <paramref name="timeProvider"/> is <see langword="null"/>.
    /// </exception>
    public CrmConnector(MonitorService monitor, IDbContextFactory<CrmDbContext> dbFactory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(dbFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _monitor = monitor;
        _dbFactory = dbFactory;
        _timeProvider = timeProvider;
        monitor.RegisterHandler("CRM", HandleInboundAsync);
    }


    // ── Outbound: CRM → ESB ──────────────────────────────────────────────

    /// <summary>Translates a CRM account update to the ESB <c>account.updated</c> event.</summary>
    public void PublishAccountUpdated(CrmAccount acct)
    {
        ArgumentNullException.ThrowIfNull(acct);
        _monitor.Publish(
            "account.updated", "CRM",
            correlationId: acct.AccountId.ToString(),
            fields: new Dictionary<string, string>
            {
                // CRM internal name   →   ESB canonical name
                ["CustomerId"] = acct.AccountNumber ?? acct.AccountId.ToString(),
                ["Name"]       = acct.Name,
                ["Email"]      = acct.EMailAddress1 ?? "",   // EMailAddress1 → Email
                ["Phone"]      = acct.Telephone1    ?? ""    // Telephone1    → Phone
            },
            isUserInitiated: true
        );
    }

    /// <summary>
    /// Translates a CRM status change to <c>account.deactivated</c> or <c>account.activated</c>.
    /// CRM uses <see cref="AccountStatusCode"/> enum; ESB uses separate topic names.
    /// </summary>
    public void PublishStatusChanged(CrmAccount acct)
    {
        ArgumentNullException.ThrowIfNull(acct);

        var topic = acct.StatusCode == AccountStatusCode.Active
            ? "account.activated"
            : "account.deactivated";

        _monitor.Publish(
            topic, "CRM",
            correlationId: acct.AccountId.ToString(),
            fields: new Dictionary<string, string>
            {
                ["CustomerId"] = acct.AccountNumber ?? acct.AccountId.ToString(),
                ["Name"]       = acct.Name
            },
            isUserInitiated: true
        );
    }


    // ── Inbound: ESB → CRM ───────────────────────────────────────────────

    private async Task HandleInboundAsync(string topic, Dictionary<string, string> fields)
    {
        if (!fields.TryGetValue("CustomerId", out var customerId) || string.IsNullOrEmpty(customerId))
            return;

        var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using var dbDisposal = db.ConfigureAwait(false);

        var acct = await db.Accounts.FirstOrDefaultAsync(
            a => a.AccountNumber == customerId
        ).ConfigureAwait(false);

        if (acct is null)
        {
            await CreateFromEsbAsync(db, topic, customerId, fields).ConfigureAwait(false);
            return;
        }

        if (string.Equals(topic, "account.updated", StringComparison.Ordinal))
        {
            // ESB Email → CRM EMailAddress1; ESB Phone → CRM Telephone1
            if (fields.TryGetValue("Name",  out var name))  acct.Name           = name;
            if (fields.TryGetValue("Email", out var email)) acct.EMailAddress1  = email;
            if (fields.TryGetValue("Phone", out var phone)) acct.Telephone1     = phone;
            acct.ModifiedOn = _timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (string.Equals(topic, "account.deactivated", StringComparison.Ordinal))
        {
            // ESB deactivated → CRM StatusCode.Inactive
            acct.StatusCode = AccountStatusCode.Inactive;
            acct.ModifiedOn = _timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (string.Equals(topic, "account.activated", StringComparison.Ordinal))
        {
            // ESB activated → CRM StatusCode.Active
            acct.StatusCode = AccountStatusCode.Active;
            acct.ModifiedOn = _timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync().ConfigureAwait(false);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    /// <summary>Creates a CRM account from ESB data when the referenced account doesn't exist yet.</summary>
    private async Task CreateFromEsbAsync(CrmDbContext db, string topic, string customerId, Dictionary<string, string> fields)
    {
        if (!string.Equals(topic, "account.created", StringComparison.Ordinal) &&
            !string.Equals(topic, "account.updated", StringComparison.Ordinal))
        {
            return;
        }

        fields.TryGetValue("Name",  out var newName);
        fields.TryGetValue("Email", out var newEmail);
        fields.TryGetValue("Phone", out var newPhone);

        var newAcct = new CrmAccount
        {
            AccountId     = Guid.NewGuid(),
            AccountNumber = customerId,
            Name          = newName ?? customerId,
            EMailAddress1 = newEmail,
            Telephone1    = newPhone,
            StatusCode    = AccountStatusCode.Active,
            CreatedOn     = _timeProvider.GetUtcNow().UtcDateTime,
            ModifiedOn    = _timeProvider.GetUtcNow().UtcDateTime
        };
        await db.Accounts.AddAsync(newAcct).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
