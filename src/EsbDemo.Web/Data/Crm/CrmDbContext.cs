using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.Crm;

/// <summary>
/// The CRM system's in-memory database.
/// </summary>
/// <param name="options">The options the context is configured with; registered in <c>Program.cs</c>.</param>
public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the CRM accounts.
    /// </summary>
    public DbSet<CrmAccount> Accounts => Set<CrmAccount>();



    /// <summary>
    /// Configures the model and seeds the CRM system's demo customers.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <see langword="null"/>.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<CrmAccount>().HasData(SeedData.Accounts);
    }
}
