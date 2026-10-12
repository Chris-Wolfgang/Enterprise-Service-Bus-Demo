using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsbDemo.Web.Data.DataWarehouse;

/// <summary>
/// A customer row in the Data Warehouse, which only receives account events from the bus.
/// </summary>
public sealed class DwCustomer
{
    /// <summary>
    /// Gets or sets the shared customer number (such as <c>CUST0001</c>), which is the bus's <c>CustomerId</c> and the primary key.
    /// </summary>
    [Key]
    [MaxLength(8)]
    public string CustNum { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer's email address.
    /// </summary>
    [MaxLength(60)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the customer's phone number.
    /// </summary>
    [MaxLength(15)]
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the customer type code.
    /// </summary>
    [MaxLength(4)]
    public string? CustType { get; set; }

    /// <summary>
    /// Gets or sets the payment terms code.
    /// </summary>
    [MaxLength(6)]
    public string? Terms { get; set; }

    /// <summary>
    /// Gets or sets the credit limit.
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal? CreditLim { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer is active. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the warehouse first received this customer, in UTC.
    /// </summary>
    public DateTime FirstSeenDate { get; set; }

    /// <summary>
    /// Gets or sets when the warehouse last applied a change to this customer, in UTC.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
