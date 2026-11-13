using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsbDemo.Web.Data.Erp;

/// <summary>
/// A row of the ERP's customer master table.
/// </summary>
/// <remarks>
/// The ERP is the system of record that mints customer numbers: it answers <c>account.create</c>
/// with <c>account.created</c>. It has no active flag; an account is closed by setting
/// <see cref="ClosedDate"/>.
/// </remarks>
public sealed class CustMast
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
    /// Gets or sets the customer type code. Defaults to <c>GEN</c>.
    /// </summary>
    [MaxLength(4)]
    public string CustType { get; set; } = "GEN";



    /// <summary>Date the account was closed. Null = active; any value = inactive.</summary>
    public DateTime? ClosedDate { get; set; }



    /// <summary>
    /// Gets a value indicating whether the account is open, that is, <see cref="ClosedDate"/> is <see langword="null"/>. Not stored.
    /// </summary>
    [NotMapped]
    public bool IsActive => ClosedDate is null;



    /// <summary>
    /// Gets or sets the payment terms code.
    /// </summary>
    [MaxLength(6)]
    public string? Terms { get; set; }



    /// <summary>
    /// Gets or sets the credit limit.
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal CreditLim { get; set; }



    /// <summary>
    /// Gets or sets the code of the assigned sales representative.
    /// </summary>
    [MaxLength(4)]
    public string? SalesRep { get; set; }



    /// <summary>
    /// Gets or sets when the row was added, in UTC.
    /// </summary>
    public DateTime AddDate { get; set; }



    /// <summary>
    /// Gets or sets when the row was last modified, in UTC.
    /// </summary>
    public DateTime ModDate { get; set; }
}
