namespace EsbDemo.Web.Data.Crm;

/// <summary>
/// The CRM's classification of an account.
/// </summary>
public enum AccountTypeCode
{
    /// <summary>A paying customer.</summary>
    Customer = 1,
    /// <summary>A potential customer that has not bought yet.</summary>
    Prospect = 2,
    /// <summary>A business partner.</summary>
    Partner = 3,
    /// <summary>A company that resells the products.</summary>
    Reseller = 4
}
