using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.CustomerPortal;

/// <summary>
/// The Customer Portal system's in-memory database.
/// </summary>
/// <param name="options">The options the context is configured with; registered in <c>Program.cs</c>.</param>
public sealed class CustomerPortalDbContext(DbContextOptions<CustomerPortalDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the portal's customer profiles.
    /// </summary>
    public DbSet<PortalCustomer> Customers => Set<PortalCustomer>();


    /// <summary>
    /// Configures the model and seeds the Customer Portal system's demo customers.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<PortalCustomer>().HasData(SeedData.Customers);
    }
}
