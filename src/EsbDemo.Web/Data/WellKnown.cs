namespace EsbDemo.Web.Data;

public static class WellKnown
{
    public static class Accounts
    {
        public static readonly Guid AcmeId    = new("a1000000-0000-0000-0000-000000000001");
        public static readonly Guid GlobexId  = new("a2000000-0000-0000-0000-000000000002");
        public static readonly Guid InitechId = new("a3000000-0000-0000-0000-000000000003");
        public static readonly Guid UmbrellaId = new("a4000000-0000-0000-0000-000000000004");
        public static readonly Guid SoylentId = new("a5000000-0000-0000-0000-000000000005");

        public static readonly string[] CustNums = ["CUST0001", "CUST0002", "CUST0003", "CUST0004", "CUST0005"];
    }
}
