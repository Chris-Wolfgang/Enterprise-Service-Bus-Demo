using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.Erp;

/// <summary>
/// The ERP system's in-memory database.
/// </summary>
/// <param name="options">The options the context is configured with; registered in <c>Program.cs</c>.</param>
public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the ERP's customer master rows.
    /// </summary>
    public DbSet<CustMast> CustMast => Set<CustMast>();


    /// <summary>
    /// Configures the model and seeds the ERP system's demo customers.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<CustMast>().HasData(SeedData.Customers);
    }
}
