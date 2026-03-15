using enBot.Models;
using Microsoft.EntityFrameworkCore;

namespace enBot.Data;

public class AppDbContext : DbContext
{
    public DbSet<PromptEntry> PromptEntries => Set<PromptEntry>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromptEntry>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Language).HasMaxLength(10);
        });
    }
}
