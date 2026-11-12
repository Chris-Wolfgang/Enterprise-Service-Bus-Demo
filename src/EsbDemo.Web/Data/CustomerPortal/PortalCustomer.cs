using System.ComponentModel.DataAnnotations;

namespace EsbDemo.Web.Data.CustomerPortal;

/// <summary>
/// A customer profile in the Customer Portal database.
/// </summary>
public sealed class PortalCustomer
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
    /// Gets or sets a value indicating whether the customer can use the portal. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IsActive { get; set; } = true;



    /// <summary>
    /// Gets or sets the customer's portal sign-in name.
    /// </summary>
    [MaxLength(50)]
    public string? PortalLogin { get; set; }



    /// <summary>
    /// Gets or sets when the customer last signed in, or <see langword="null"/> if never.
    /// </summary>
    public DateTime? LastLoginDate { get; set; }



    /// <summary>
    /// Gets or sets when the profile was created, in UTC.
    /// </summary>
    public DateTime CreatedDate { get; set; }
}
