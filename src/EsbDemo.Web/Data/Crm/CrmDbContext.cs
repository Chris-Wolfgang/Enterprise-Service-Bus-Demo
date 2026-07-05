using Microsoft.EntityFrameworkCore;

namespace EsbDemo.Web.Data.Crm;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<CrmAccount> Accounts => Set<CrmAccount>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<CrmAccount>().HasData(SeedData.Accounts);
    }
}
