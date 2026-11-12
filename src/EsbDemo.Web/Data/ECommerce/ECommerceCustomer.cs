using System.ComponentModel.DataAnnotations;

namespace EsbDemo.Web.Data.ECommerce;

/// <summary>
/// A customer in the eCommerce store's database.
/// </summary>
public sealed class ECommerceCustomer
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
    /// Gets or sets a value indicating whether the customer can place orders. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IsActive { get; set; } = true;



    /// <summary>
    /// Gets or sets when the customer was created, in UTC.
    /// </summary>
    public DateTime CreatedDate { get; set; }
}
