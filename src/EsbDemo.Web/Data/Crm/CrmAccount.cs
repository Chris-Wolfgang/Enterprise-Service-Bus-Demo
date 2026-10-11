using System.ComponentModel.DataAnnotations;

namespace EsbDemo.Web.Data.Crm;

/// <summary>
/// An account row in the CRM database, modelled on a typical CRM account entity.
/// </summary>
/// <remarks>
/// The CRM uses its own field names; <see cref="Connectors.CrmConnector"/> maps them to and from the
/// bus's canonical names (for example <see cref="EMailAddress1"/> to <c>Email</c>).
/// </remarks>
public sealed class CrmAccount
{
    /// <summary>
    /// Gets or sets the CRM's internal primary key.
    /// </summary>
    [Key]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Gets or sets the shared customer number (such as <c>CUST0001</c>), which is the bus's <c>CustomerId</c>.
    /// </summary>
    [MaxLength(8)]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the account name.
    /// </summary>
    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary email address; the bus's <c>Email</c> field.
    /// </summary>
    [MaxLength(100)]
    public string? EMailAddress1 { get; set; }

    /// <summary>
    /// Gets or sets the primary phone number; the bus's <c>Phone</c> field.
    /// </summary>
    [MaxLength(50)]
    public string? Telephone1 { get; set; }

    /// <summary>
    /// Gets or sets the account's classification. Defaults to <see cref="Crm.AccountTypeCode.Customer"/>.
    /// </summary>
    public AccountTypeCode AccountTypeCode { get; set; } = AccountTypeCode.Customer;

    /// <summary>
    /// Gets or sets the account's status. Defaults to <see cref="AccountStatusCode.Active"/>.
    /// </summary>
    public AccountStatusCode StatusCode { get; set; } = AccountStatusCode.Active;

    /// <summary>
    /// Gets or sets when the account was created, in UTC.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when the account was last modified, in UTC.
    /// </summary>
    public DateTime ModifiedOn { get; set; }
}
