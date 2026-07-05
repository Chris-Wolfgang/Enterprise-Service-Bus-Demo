using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.CustomerPortal;

public sealed class CustomerPortalDbContext(DbContextOptions<CustomerPortalDbContext> options) : DbContext(options)
{
    public DbSet<PortalCustomer> Customers => Set<PortalCustomer>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<PortalCustomer>().HasData(SeedData.Customers);
    }
}
