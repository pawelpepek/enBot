using enBot.Data;
using enBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace enBot.Services;

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

    public async Task<PromptsStatistics> GetAllStatsAsync()
    {
        await using var ctx = CreateContext();
        var dailyRaw = await ctx.PromptEntries
            .GroupBy(e => e.ReceivedAt.Date)
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
                Date = d.Date,
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
