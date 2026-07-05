using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.DataWarehouse;

public sealed class DataWarehouseDbContext(DbContextOptions<DataWarehouseDbContext> options) : DbContext(options)
{
    public DbSet<DwCustomer> Customers => Set<DwCustomer>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<DwCustomer>().HasData(SeedData.Customers);
    }
}
