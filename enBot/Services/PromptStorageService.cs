using enBot.Data;
using enBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace enBot.Services;

public record DailyStat(DateTime Date, int Count, double AvgScore, double AvgComplexity);
public record MonthlyStat(int Year, int Month, int Count, double AvgScore, double AvgComplexity);

public class PromptStorageService
{
    private readonly DbContextOptions<AppDbContext> _options;

    public PromptStorageService(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    private AppDbContext CreateContext() => new(_options);

    public async Task InitializeAsync()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
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
            Language = payload.Language,
            Complexity = payload.Complexity,
            HookVersion = payload.HookVersion,
            ExplanationsJson = payload.Explanations is { Count: > 0 }
                ? JsonSerializer.Serialize(payload.Explanations)
                : null,
            ReceivedAt = DateTime.UtcNow
        };
        ctx.PromptEntries.Add(entry);
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<int> GetTotalPromptsAsync()
    {
        await using var ctx = CreateContext();
        return await ctx.PromptEntries.CountAsync().ConfigureAwait(false);
    }

    public async Task<double> GetAverageScoreAsync()
    {
        await using var ctx = CreateContext();
        if (!await ctx.PromptEntries.AnyAsync().ConfigureAwait(false)) return 0;
        return await ctx.PromptEntries.AverageAsync(e => (double)e.Score).ConfigureAwait(false);
    }

    public async Task<double> GetAverageComplexityAsync()
    {
        await using var ctx = CreateContext();
        if (!await ctx.PromptEntries.AnyAsync().ConfigureAwait(false)) return 0;
        return await ctx.PromptEntries.AverageAsync(e => (double)e.Complexity).ConfigureAwait(false);
    }

    public async Task<double> GetAverageWeightedComplexityAsync()
    {
        await using var ctx = CreateContext();
        var entries = await ctx.PromptEntries
            .Select(e => new { e.Complexity, e.WordCount })
            .ToListAsync().ConfigureAwait(false);

        if (entries.Count == 0) return 0;
        var totalWords = entries.Sum(e => e.WordCount);
        if (totalWords == 0) return 0;
        return entries.Sum(e => (double)e.Complexity * e.WordCount) / totalWords;
    }

    public async Task<double> GetAverageWeightedScoreAsync()
    {
        await using var ctx = CreateContext();
        var entries = await ctx.PromptEntries
            .Select(e => new { e.Score, e.WordCount })
            .ToListAsync().ConfigureAwait(false);

        if (entries.Count == 0) return 0;
        var totalWords = entries.Sum(e => e.WordCount);
        if (totalWords == 0) return 0;
        return entries.Sum(e => (double)e.Score * e.WordCount) / totalWords;
    }

    public async Task<List<DailyStat>> GetAllDailyStatsAsync()
    {
        await using var ctx = CreateContext();
        var entries = await ctx.PromptEntries
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync().ConfigureAwait(false);

        return entries
            .GroupBy(e => e.ReceivedAt.Date)
            .Select(g => new DailyStat(
                g.Key,
                g.Count(),
                g.Average(e => (double)e.Score),
                g.Average(e => (double)e.Complexity)
            ))
            .OrderBy(s => s.Date)
            .ToList();
    }

    public async Task<List<DailyStat>> GetWeeklyStatsAsync()
    {
        var since = DateTime.UtcNow.Date.AddDays(-6);
        await using var ctx = CreateContext();
        var entries = await ctx.PromptEntries
            .Where(e => e.ReceivedAt >= since)
            .ToListAsync().ConfigureAwait(false);

        return Enumerable.Range(0, 7)
            .Select(i => since.AddDays(i))
            .Select(day =>
            {
                var dayEntries = entries.Where(e => e.ReceivedAt.Date == day).ToList();
                return new DailyStat(
                    day,
                    dayEntries.Count,
                    dayEntries.Count > 0 ? dayEntries.Average(e => (double)e.Score) : 0,
                    dayEntries.Count > 0 ? dayEntries.Average(e => (double)e.Complexity) : 0
                );
            })
            .ToList();
    }

    public async Task<List<DailyStat>> GetMonthlyStatsAsync()
    {
        var since = DateTime.UtcNow.Date.AddDays(-29);
        await using var ctx = CreateContext();
        var entries = await ctx.PromptEntries
            .Where(e => e.ReceivedAt >= since)
            .ToListAsync().ConfigureAwait(false);

        return Enumerable.Range(0, 30)
            .Select(i => since.AddDays(i))
            .Select(day =>
            {
                var dayEntries = entries.Where(e => e.ReceivedAt.Date == day).ToList();
                return new DailyStat(
                    day,
                    dayEntries.Count,
                    dayEntries.Count > 0 ? dayEntries.Average(e => (double)e.Score) : 0,
                    dayEntries.Count > 0 ? dayEntries.Average(e => (double)e.Complexity) : 0
                );
            })
            .ToList();
    }

    public async Task<List<MonthlyStat>> GetYearlyStatsAsync()
    {
        var since = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-11);
        await using var ctx = CreateContext();
        var entries = await ctx.PromptEntries
            .Where(e => e.ReceivedAt >= since)
            .ToListAsync().ConfigureAwait(false);

        return Enumerable.Range(0, 12)
            .Select(i => since.AddMonths(i))
            .Select(month =>
            {
                var monthEntries = entries
                    .Where(e => e.ReceivedAt.Year == month.Year && e.ReceivedAt.Month == month.Month)
                    .ToList();
                return new MonthlyStat(
                    month.Year,
                    month.Month,
                    monthEntries.Count,
                    monthEntries.Count > 0 ? monthEntries.Average(e => (double)e.Score) : 0,
                    monthEntries.Count > 0 ? monthEntries.Average(e => (double)e.Complexity) : 0
                );
            })
            .ToList();
    }

    public async Task<List<PromptEntry>> GetPromptsForMonthAsync(int year, int month)
    {
        await using var ctx = CreateContext();
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return await ctx.PromptEntries
            .Where(e => e.ReceivedAt >= start && e.ReceivedAt < end)
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync().ConfigureAwait(false);
    }
}
