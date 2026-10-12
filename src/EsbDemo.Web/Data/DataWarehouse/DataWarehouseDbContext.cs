using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.DataWarehouse;

/// <summary>
/// The Data Warehouse system's in-memory database.
/// </summary>
/// <param name="options">The options the context is configured with; registered in <c>Program.cs</c>.</param>
public sealed class DataWarehouseDbContext(DbContextOptions<DataWarehouseDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the warehouse's customer dimension rows.
    /// </summary>
    public DbSet<DwCustomer> Customers => Set<DwCustomer>();


    /// <summary>
    /// Configures the model and seeds the Data Warehouse system's demo customers.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<DwCustomer>().HasData(SeedData.Customers);
    }
}
