using enBot.Models;
using Microsoft.EntityFrameworkCore;

namespace enBot.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PromptEntry> PromptEntries => Set<PromptEntry>();
    public DbSet<PromptSuggestion> PromptSuggestions => Set<PromptSuggestion>();
    public DbSet<AppStateEntry> AppState => Set<AppStateEntry>();
    public DbSet<ProgressReport> ProgressReports => Set<ProgressReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromptEntry>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.ReceivedDay);
        });
        modelBuilder.Entity<PromptSuggestion>(e => e.HasKey(s => s.Id));
        modelBuilder.Entity<AppStateEntry>(e => e.HasKey(s => s.Key));
        modelBuilder.Entity<ProgressReport>(e => e.HasKey(r => r.Id));
    }
}
