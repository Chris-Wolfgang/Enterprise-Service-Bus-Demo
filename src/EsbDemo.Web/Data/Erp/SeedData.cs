namespace EsbDemo.Web.Data.Erp;

internal static class SeedData
{
    internal static readonly CustMast[] Customers =
    [
        new() { CustNum = "CUST0001", Name = "Acme Corporation",   Email = "contact@acme.com",    Phone = "555-1000", CustType = "RETL", ClosedDate = null,                         Terms = "NET30", CreditLim = 50000m,  SalesRep = "JDoe", AddDate = new DateTime(2024, 1, 10), ModDate = new DateTime(2024, 1, 10) },
        new() { CustNum = "CUST0002", Name = "Globex Industries",  Email = "info@globex.com",     Phone = "555-2000", CustType = "WHSL", ClosedDate = null,                         Terms = "NET30", CreditLim = 100000m, SalesRep = "JDoe", AddDate = new DateTime(2024, 2, 15), ModDate = new DateTime(2024, 2, 15) },
        new() { CustNum = "CUST0003", Name = "Initech Solutions",  Email = "support@initech.com", Phone = "555-3000", CustType = "GEN",  ClosedDate = null,                         Terms = "NET15", CreditLim = 25000m,  SalesRep = "BSmt", AddDate = new DateTime(2024, 3, 5),  ModDate = new DateTime(2024, 3, 5)  },
        new() { CustNum = "CUST0004", Name = "Umbrella Corp",      Email = "orders@umbrella.com", Phone = "555-4000", CustType = "GOV",  ClosedDate = null,                         Terms = "NET60", CreditLim = 200000m, SalesRep = "BSmt", AddDate = new DateTime(2024, 4, 20), ModDate = new DateTime(2024, 4, 20) },
        new() { CustNum = "CUST0005", Name = "Soylent Green LLC",  Email = "sales@soylent.com",   Phone = "555-5000", CustType = "RETL", ClosedDate = new DateTime(2024, 12, 31),   Terms = "COD",   CreditLim = 10000m,  SalesRep = "JDoe", AddDate = new DateTime(2024, 5, 1),  ModDate = new DateTime(2024, 5, 1)  }
    ];
}
