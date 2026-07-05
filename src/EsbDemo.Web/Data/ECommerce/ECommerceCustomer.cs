using System.ComponentModel.DataAnnotations;

namespace EsbDemo.Web.Data.ECommerce;

public sealed class ECommerceCustomer
{
    [Key]
    [MaxLength(8)]
    public string CustNum { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? Email { get; set; }

    [MaxLength(15)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }
}
