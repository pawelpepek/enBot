using enBot.Data;
using enBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace enBot.Services.Infrastructure;

public class AppRepository
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AppRepository(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    private AppDbContext CreateContext() => new(_options);

    public async Task InitializeAsync()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PromptSuggestions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PromptSuggestions" PRIMARY KEY AUTOINCREMENT,
                "CreatedAt" TEXT NOT NULL,
                "SuggestionText" TEXT NOT NULL,
                "ExplanationText" TEXT NOT NULL DEFAULT ''
            )
            """).ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AppState" (
                "Key" INTEGER NOT NULL CONSTRAINT "PK_AppState" PRIMARY KEY,
                "Value" INTEGER NOT NULL DEFAULT 0
            )
            """).ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ProgressReports" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ProgressReports" PRIMARY KEY AUTOINCREMENT,
                "CreatedAt" TEXT NOT NULL,
                "ReportText" TEXT NOT NULL,
                "SincePromptId" INTEGER NOT NULL DEFAULT 0
            )
            """).ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AppConfig" (
                "Key" INTEGER NOT NULL CONSTRAINT "PK_AppConfig" PRIMARY KEY,
                "Value" TEXT NOT NULL DEFAULT ''
            )
            """).ConfigureAwait(false);

        try
        {
            await ctx.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "PromptEntries" ADD COLUMN "ReceivedDay" TEXT NOT NULL DEFAULT ''
                """).ConfigureAwait(false);
        }
        catch { /* column already exists on fresh DBs created by EnsureCreatedAsync */ }

        try
        {
            await ctx.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "PromptEntries" ADD COLUMN "BetterVersion" TEXT
                """).ConfigureAwait(false);
        }
        catch { /* column already exists */ }

        await ctx.Database.ExecuteSqlRawAsync("""
            UPDATE "PromptEntries" SET "ReceivedDay" = strftime('%Y-%m-%d', "ReceivedAt") WHERE "ReceivedDay" = ''
            """).ConfigureAwait(false);

        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_PromptEntries_ReceivedDay" ON "PromptEntries" ("ReceivedDay")
            """).ConfigureAwait(false);
    }

    public async Task SavePromptAsync(HookPayload payload)
    {
        await using var ctx = CreateContext();
        var entry = new PromptEntry
        {
            Original = payload.Original,
            Corrected = payload.Corrected,
            Score = payload.Score,
            WordCount = payload.WordCount,
            Complexity = payload.Complexity,
            HookVersion = payload.HookVersion,
            ExplanationsJson = payload.Explanations is { Count: > 0 }
                ? JsonSerializer.Serialize(payload.Explanations)
                : null,
            BetterVersion = payload.BetterVersion,
            ReceivedAt = DateTime.Now,
            ReceivedDay = DateTime.Now.ToString("yyyy-MM-dd")
        };
        ctx.PromptEntries.Add(entry);
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<int> GetPromptCountSinceAsync(int afterPromptId)
    {
        await using var ctx = CreateContext();
        return await ctx.PromptEntries
            .CountAsync(e => e.Id > afterPromptId)
            .ConfigureAwait(false);
    }

    public async Task<int> GetSuggestionCountAsync()
    {
        await using var ctx = CreateContext();
        return await ctx.PromptSuggestions.CountAsync().ConfigureAwait(false);
    }

    public async Task<int> GetLastPromptIdAsync()
    {
        await using var ctx = CreateContext();
        return await ctx.PromptEntries
            .OrderByDescending(e => e.Id)
            .Select(e => e.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<PromptEntry>> GetLastPromptsAsync(int count)
    {
        await using var ctx = CreateContext();
        return await ctx.PromptEntries
            .OrderByDescending(e => e.Id)
            .Take(count)
            .ToListAsync()
            .ConfigureAwait(false);
    }


    public async Task<List<PromptSuggestion>> GetRecentSuggestionsAsync(int count)
    {
        await using var ctx = CreateContext();
        return await ctx.PromptSuggestions
            .OrderByDescending(s => s.Id)
            .Take(count)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task SaveSuggestionAsync(PromptSuggestion suggestion)
    {
        await using var ctx = CreateContext();
        ctx.PromptSuggestions.Add(suggestion);
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<int> GetStateAsync(AppStateKey key)
    {
        await using var ctx = CreateContext();
        var entry = await ctx.AppState.FindAsync(key).ConfigureAwait(false);
        return entry?.Value ?? 0;
    }

    public async Task SetStateAsync(AppStateKey key, int value)
    {
        await using var ctx = CreateContext();
        var entry = await ctx.AppState.FindAsync(key).ConfigureAwait(false);
        if (entry is null)
            ctx.AppState.Add(new AppStateEntry { Key = key, Value = value });
        else
            entry.Value = value;
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<string> GetConfigAsync(AppConfigKey key)
    {
        await using var ctx = CreateContext();
        var entry = await ctx.AppConfig.FindAsync(key).ConfigureAwait(false);
        return entry?.Value ?? "";
    }

    public async Task SetConfigAsync(AppConfigKey key, string value)
    {
        await using var ctx = CreateContext();
        var entry = await ctx.AppConfig.FindAsync(key).ConfigureAwait(false);
        if (entry is null)
            ctx.AppConfig.Add(new AppConfigEntry { Key = key, Value = value });
        else
            entry.Value = value;
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SaveReportAsync(ProgressReport report)
    {
        await using var ctx = CreateContext();
        ctx.ProgressReports.Add(report);
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<List<ProgressReport>> GetRecentReportsAsync(int count)
    {
        await using var ctx = CreateContext();
        return await ctx.ProgressReports
            .OrderByDescending(r => r.Id)
            .Take(count)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<PromptsStatistics> GetAllStatsAsync()
    {
        await using var ctx = CreateContext();
        var dailyRaw = await ctx.PromptEntries
            .GroupBy(e => e.ReceivedDay)
            .Select(g => new
            {
                Date = g.Key,
                TotalPrompts = g.Count(),
                TotalWordCount = g.Sum(e => e.WordCount),
                WeightedScoreSum = g.Sum(e => e.Score * e.WordCount),
                WeightedComplexitySum = g.Sum(e => e.Complexity * e.WordCount)
            })
            .OrderBy(g => g.Date)
            .ToListAsync()
            .ConfigureAwait(false);

        var dailyStatistics = dailyRaw
            .Select(d => new DayPromptsStatistics
            {
                Date = DateTime.ParseExact(d.Date, "yyyy-MM-dd", null),
                TotalPrompts = d.TotalPrompts,
                AvgWeightedScore = d.TotalWordCount > 0 ? (double)d.WeightedScoreSum / d.TotalWordCount : 0,
                AvgWeightedComplexity = d.TotalWordCount > 0 ? (double)d.WeightedComplexitySum / d.TotalWordCount : 0
            })
            .ToList();

        var totalWordCount = dailyRaw.Sum(d => d.TotalWordCount);
        return new PromptsStatistics
        {
            TotalPrompts = dailyRaw.Sum(d => d.TotalPrompts),
            AvgWeightedScore = totalWordCount > 0 ? (double)dailyRaw.Sum(d => d.WeightedScoreSum) / totalWordCount : 0,
            AvgWeightedComplexity = totalWordCount > 0 ? (double)dailyRaw.Sum(d => d.WeightedComplexitySum) / totalWordCount : 0,
            DailyStatistics = dailyStatistics
        };
    }
}
