using Microsoft.EntityFrameworkCore;
using NetForge.Core.Models;
using NetForge.Core.Configuration;
using NetForge.Core.Interfaces;

namespace NetForge.Core.Data;

public class NetForgeDbContext : DbContext
{
    public NetForgeDbContext(DbContextOptions<NetForgeDbContext> options) : base(options)
    {
    }

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<ITrackable>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            entityEntry.Entity.UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
    }
}