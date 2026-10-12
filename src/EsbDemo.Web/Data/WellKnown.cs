namespace EsbDemo.Web.Data;

/// <summary>
/// Fixed identifiers shared by the seed data of every system.
/// </summary>
public static class WellKnown
{
    /// <summary>
    /// Identifiers of the five demo customers that every system is seeded with.
    /// </summary>
    public static class Accounts
    {
        /// <summary>
        /// The CRM <see cref="Crm.CrmAccount.AccountId"/> of Acme Corporation (<c>CUST0001</c>).
        /// </summary>
        public static readonly Guid AcmeId    = new("a1000000-0000-0000-0000-000000000001");
        /// <summary>
        /// The CRM <see cref="Crm.CrmAccount.AccountId"/> of Globex Industries (<c>CUST0002</c>).
        /// </summary>
        public static readonly Guid GlobexId  = new("a2000000-0000-0000-0000-000000000002");
        /// <summary>
        /// The CRM <see cref="Crm.CrmAccount.AccountId"/> of Initech Solutions (<c>CUST0003</c>).
        /// </summary>
        public static readonly Guid InitechId = new("a3000000-0000-0000-0000-000000000003");
        /// <summary>
        /// The CRM <see cref="Crm.CrmAccount.AccountId"/> of Umbrella Corp (<c>CUST0004</c>).
        /// </summary>
        public static readonly Guid UmbrellaId = new("a4000000-0000-0000-0000-000000000004");
        /// <summary>
        /// The CRM <see cref="Crm.CrmAccount.AccountId"/> of Soylent Green LLC (<c>CUST0005</c>).
        /// </summary>
        public static readonly Guid SoylentId = new("a5000000-0000-0000-0000-000000000005");

        /// <summary>
        /// The shared customer numbers of the five demo customers, in the same order as the IDs above.
        /// </summary>
        public static readonly string[] CustNums = ["CUST0001", "CUST0002", "CUST0003", "CUST0004", "CUST0005"];
    }
}
