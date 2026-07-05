namespace EsbDemo.Web.Data.CustomerPortal;

internal static class SeedData
{
    internal static readonly PortalCustomer[] Customers =
    [
        new() { CustNum = "CUST0001", Name = "Acme Corporation",  Email = "contact@acme.com",    IsActive = true, PortalLogin = "acme.admin",    LastLoginDate = new DateTime(2025, 6, 1),  CreatedDate = new DateTime(2024, 1, 10) },
        new() { CustNum = "CUST0002", Name = "Globex Industries", Email = "info@globex.com",     IsActive = true, PortalLogin = "globex.user",   LastLoginDate = new DateTime(2025, 6, 10), CreatedDate = new DateTime(2024, 2, 15) },
        new() { CustNum = "CUST0003", Name = "Initech Solutions", Email = "support@initech.com", IsActive = true, PortalLogin = "initech.user",  LastLoginDate = null,                      CreatedDate = new DateTime(2024, 3, 5)  },
        new() { CustNum = "CUST0004", Name = "Umbrella Corp",     Email = "orders@umbrella.com", IsActive = true, PortalLogin = "umbrella.mgr",  LastLoginDate = new DateTime(2025, 5, 28), CreatedDate = new DateTime(2024, 4, 20) },
        new() { CustNum = "CUST0005", Name = "Soylent Green LLC", Email = "sales@soylent.com",   IsActive = false, PortalLogin = "soylent.sales", LastLoginDate = new DateTime(2024, 12, 15), CreatedDate = new DateTime(2024, 5, 1)  }
    ];
}
