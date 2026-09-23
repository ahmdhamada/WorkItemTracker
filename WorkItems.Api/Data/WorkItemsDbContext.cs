using Microsoft.EntityFrameworkCore;
using WorkItems.Api.Models;

namespace WorkItems.Api.Data;

public sealed class WorkItemsDbContext(DbContextOptions<WorkItemsDbContext> options) : DbContext(options)
{
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var item = modelBuilder.Entity<WorkItem>();
        item.Property(x => x.Title).HasMaxLength(120).IsRequired();
        item.Property(x => x.Description).HasMaxLength(4000);
        item.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        item.Property(x => x.CreatedAt).IsRequired();
        item.HasIndex(x => x.CreatedAt);
    }
}
