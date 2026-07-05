using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.ECommerce;

public sealed class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{
    public DbSet<ECommerceCustomer> Customers => Set<ECommerceCustomer>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<ECommerceCustomer>().HasData(SeedData.Customers);
    }
}
