using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsbDemo.Web.Data.DataWarehouse;

public sealed class DwCustomer
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

    [MaxLength(4)]
    public string? CustType { get; set; }

    [MaxLength(6)]
    public string? Terms { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? CreditLim { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime FirstSeenDate { get; set; }

    public DateTime LastUpdated { get; set; }
}
