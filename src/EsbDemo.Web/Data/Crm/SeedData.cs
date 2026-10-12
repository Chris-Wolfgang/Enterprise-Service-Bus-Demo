namespace EsbDemo.Web.Data.Crm;

internal static class SeedData
{
    internal static readonly CrmAccount[] Accounts =
    [
        new()
        {
            AccountId = WellKnown.Accounts.AcmeId,
            AccountNumber = "CUST0001",
            Name = "Acme Corporation",
            EMailAddress1 = "contact@acme.com",
            Telephone1 = "555-1000",
            AccountTypeCode = AccountTypeCode.Customer,
            StatusCode = AccountStatusCode.Active,
            CreatedOn = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            ModifiedOn = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            AccountId = WellKnown.Accounts.GlobexId,
            AccountNumber = "CUST0002",
            Name = "Globex Industries",
            EMailAddress1 = "info@globex.com",
            Telephone1 = "555-2000",
            AccountTypeCode = AccountTypeCode.Customer,
            StatusCode = AccountStatusCode.Active,
            CreatedOn = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            ModifiedOn = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            AccountId = WellKnown.Accounts.InitechId,
            AccountNumber = "CUST0003",
            Name = "Initech Solutions",
            EMailAddress1 = "support@initech.com",
            Telephone1 = "555-3000",
            AccountTypeCode = AccountTypeCode.Prospect,
            StatusCode = AccountStatusCode.Active,
            CreatedOn = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc),
            ModifiedOn = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            AccountId = WellKnown.Accounts.UmbrellaId,
            AccountNumber = "CUST0004",
            Name = "Umbrella Corp",
            EMailAddress1 = "orders@umbrella.com",
            Telephone1 = "555-4000",
            AccountTypeCode = AccountTypeCode.Customer,
            StatusCode = AccountStatusCode.Active,
            CreatedOn = new DateTime(2024, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            ModifiedOn = new DateTime(2024, 4, 20, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            AccountId = WellKnown.Accounts.SoylentId,
            AccountNumber = "CUST0005",
            Name = "Soylent Green LLC",
            EMailAddress1 = "sales@soylent.com",
            Telephone1 = "555-5000",
            AccountTypeCode = AccountTypeCode.Reseller,
            // Inactive everywhere — ERP closed this account 2024-12-31
            StatusCode = AccountStatusCode.Inactive,
            CreatedOn = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            ModifiedOn = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        }
    ];
}
