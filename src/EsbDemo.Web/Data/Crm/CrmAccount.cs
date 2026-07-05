using System.ComponentModel.DataAnnotations;

namespace EsbDemo.Web.Data.Crm;

public sealed class CrmAccount
{
    [Key]
    public Guid AccountId { get; set; }

    [MaxLength(8)]
    public string? AccountNumber { get; set; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EMailAddress1 { get; set; }

    [MaxLength(50)]
    public string? Telephone1 { get; set; }

    public AccountTypeCode AccountTypeCode { get; set; } = AccountTypeCode.Customer;

    public AccountStatusCode StatusCode { get; set; } = AccountStatusCode.Active;

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }
}
