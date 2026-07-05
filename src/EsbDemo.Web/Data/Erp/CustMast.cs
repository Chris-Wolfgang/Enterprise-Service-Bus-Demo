using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsbDemo.Web.Data.Erp;

public sealed class CustMast
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
    public string CustType { get; set; } = "GEN";

    /// <summary>Date the account was closed. Null = active; any value = inactive.</summary>
    public DateTime? ClosedDate { get; set; }

    [NotMapped]
    public bool IsActive => ClosedDate is null;

    [MaxLength(6)]
    public string? Terms { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal CreditLim { get; set; }

    [MaxLength(4)]
    public string? SalesRep { get; set; }

    public DateTime AddDate { get; set; }

    public DateTime ModDate { get; set; }
}
