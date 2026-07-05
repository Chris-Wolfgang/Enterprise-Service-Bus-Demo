using System.ComponentModel.DataAnnotations;

namespace EsbDemo.Web.Data.CustomerPortal;

public sealed class PortalCustomer
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

    [MaxLength(50)]
    public string? PortalLogin { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public DateTime CreatedDate { get; set; }
}
