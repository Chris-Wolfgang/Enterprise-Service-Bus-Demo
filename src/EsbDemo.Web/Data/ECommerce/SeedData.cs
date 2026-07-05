namespace EsbDemo.Web.Data.ECommerce;

internal static class SeedData
{
    internal static readonly ECommerceCustomer[] Customers =
    [
        new() { CustNum = "CUST0001", Name = "Acme Corporation",  Email = "contact@acme.com",    Phone = "555-1000", IsActive = true, CreatedDate = new DateTime(2024, 1, 10) },
        new() { CustNum = "CUST0002", Name = "Globex Industries", Email = "info@globex.com",     Phone = "555-2000", IsActive = true, CreatedDate = new DateTime(2024, 2, 15) },
        new() { CustNum = "CUST0003", Name = "Initech Solutions", Email = "support@initech.com", Phone = "555-3000", IsActive = true, CreatedDate = new DateTime(2024, 3, 5)  },
        new() { CustNum = "CUST0004", Name = "Umbrella Corp",     Email = "orders@umbrella.com", Phone = "555-4000", IsActive = true, CreatedDate = new DateTime(2024, 4, 20) },
        new() { CustNum = "CUST0005", Name = "Soylent Green LLC", Email = "sales@soylent.com",   Phone = "555-5000", IsActive = false, CreatedDate = new DateTime(2024, 5, 1)  }
    ];
}
