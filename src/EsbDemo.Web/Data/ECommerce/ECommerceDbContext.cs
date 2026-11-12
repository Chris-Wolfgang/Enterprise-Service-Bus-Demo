using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.ECommerce;

/// <summary>
/// The eCommerce system's in-memory database.
/// </summary>
/// <param name="options">The options the context is configured with; registered in <c>Program.cs</c>.</param>
public sealed class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the store's customers.
    /// </summary>
    public DbSet<ECommerceCustomer> Customers => Set<ECommerceCustomer>();



    /// <summary>
    /// Configures the model and seeds the eCommerce system's demo customers.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<ECommerceCustomer>().HasData(SeedData.Customers);
    }
}
