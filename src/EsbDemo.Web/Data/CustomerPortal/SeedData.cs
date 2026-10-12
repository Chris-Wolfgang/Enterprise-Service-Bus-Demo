namespace EsbDemo.Web.Data.CustomerPortal;

internal static class SeedData
{
    internal static readonly PortalCustomer[] Customers =
    [
        new() { CustNum = "CUST0001", Name = "Acme Corporation",  Email = "contact@acme.com",    Phone = "555-1000", IsActive = true, PortalLogin = "acme.admin",    LastLoginDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),  CreatedDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
        new() { CustNum = "CUST0002", Name = "Globex Industries", Email = "info@globex.com",     Phone = "555-2000", IsActive = true, PortalLogin = "globex.user",   LastLoginDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc), CreatedDate = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc) },
        new() { CustNum = "CUST0003", Name = "Initech Solutions", Email = "support@initech.com", Phone = "555-3000", IsActive = true, PortalLogin = "initech.user",  LastLoginDate = null,                      CreatedDate = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc)  },
        new() { CustNum = "CUST0004", Name = "Umbrella Corp",     Email = "orders@umbrella.com", Phone = "555-4000", IsActive = true, PortalLogin = "umbrella.mgr",  LastLoginDate = new DateTime(2025, 5, 28, 0, 0, 0, DateTimeKind.Utc), CreatedDate = new DateTime(2024, 4, 20, 0, 0, 0, DateTimeKind.Utc) },
        new() { CustNum = "CUST0005", Name = "Soylent Green LLC", Email = "sales@soylent.com",   Phone = "555-5000", IsActive = false, PortalLogin = "soylent.sales", LastLoginDate = new DateTime(2024, 12, 15, 0, 0, 0, DateTimeKind.Utc), CreatedDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc)  }
    ];
}
