using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.Erp;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<CustMast> CustMast => Set<CustMast>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<CustMast>().HasData(SeedData.Customers);
    }
}
