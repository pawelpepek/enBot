using enBot.Data;
using enBot.Models;
using enBot.Services.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace enBot.Tests.Integration;

public class AppRepositoryTests : IAsyncLifetime
{
    private AppRepository _repo = null!;
    private DbContextOptions<AppDbContext> _options = null!;
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        // Keep a single shared connection open so the in-memory DB persists
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var ctx = new AppDbContext(_options);
        await ctx.Database.EnsureCreatedAsync();

        _repo = new AppRepository(_options);
        await _repo.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private static HookPayload MakePayload(int score, int complexity, int wordCount, string day = "2025-01-01") =>
        new()
        {
            Original = "test",
            Corrected = "",
            DisplayOriginal = "test",
            Score = score,
            Complexity = complexity,
            WordCount = wordCount,
            Explanations = [],
            HookVersion = "2.0"
        };

    [Fact]
    public async Task GetAllStatsAsync_EmptyDb_ReturnsZeros()
    {
        var stats = await _repo.GetAllStatsAsync();
        Assert.Equal(0, stats.TotalPrompts);
        Assert.Equal(0, stats.AvgWeightedScore);
        Assert.Equal(0, stats.AvgWeightedComplexity);
        Assert.Empty(stats.DailyStatistics);
    }

    [Fact]
    public async Task GetAllStatsAsync_SinglePrompt_ScoreEqualsWeightedScore()
    {
        await _repo.SavePromptAsync(MakePayload(score: 8, complexity: 6, wordCount: 10));
        var stats = await _repo.GetAllStatsAsync();
        Assert.Equal(1, stats.TotalPrompts);
        Assert.Equal(8.0, stats.AvgWeightedScore);
        Assert.Equal(6.0, stats.AvgWeightedComplexity);
    }

    [Fact]
    public async Task GetAllStatsAsync_TwoPromptsEqualWords_WeightedAverageIsArithmetic()
    {
        await _repo.SavePromptAsync(MakePayload(score: 6, complexity: 4, wordCount: 10));
        await _repo.SavePromptAsync(MakePayload(score: 8, complexity: 6, wordCount: 10));
        var stats = await _repo.GetAllStatsAsync();
        Assert.Equal(2, stats.TotalPrompts);
        Assert.Equal(7.0, stats.AvgWeightedScore);
        Assert.Equal(5.0, stats.AvgWeightedComplexity);
    }

    [Fact]
    public async Task GetAllStatsAsync_DifferentWordCounts_AppliesWeights()
    {
        // score 10 with 10 words, score 2 with 2 words → weighted = (100+4)/12 = 8.67
        await _repo.SavePromptAsync(MakePayload(score: 10, complexity: 8, wordCount: 10));
        await _repo.SavePromptAsync(MakePayload(score: 2, complexity: 2, wordCount: 2));
        var stats = await _repo.GetAllStatsAsync();
        Assert.Equal(2, stats.TotalPrompts);
        Assert.Equal((10 * 10 + 2 * 2) / 12.0, stats.AvgWeightedScore, precision: 10);
    }

    [Fact]
    public async Task SavePromptAsync_ThenGetLastPrompts_ReturnsEntry()
    {
        await _repo.SavePromptAsync(MakePayload(score: 9, complexity: 7, wordCount: 5));
        var prompts = await _repo.GetLastPromptsAsync(10);
        Assert.Single(prompts);
        Assert.Equal(9, prompts[0].Score);
    }

    [Fact]
    public async Task GetLastPromptIdAsync_AfterSave_ReturnsNonZero()
    {
        await _repo.SavePromptAsync(MakePayload(score: 7, complexity: 5, wordCount: 3));
        var id = await _repo.GetLastPromptIdAsync();
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetPromptCountSinceAsync_CountsOnlyAfterGivenId()
    {
        await _repo.SavePromptAsync(MakePayload(score: 7, complexity: 5, wordCount: 3));
        var id = await _repo.GetLastPromptIdAsync();
        await _repo.SavePromptAsync(MakePayload(score: 8, complexity: 6, wordCount: 4));
        await _repo.SavePromptAsync(MakePayload(score: 9, complexity: 7, wordCount: 5));

        var count = await _repo.GetPromptCountSinceAsync(id);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task SetStateAsync_ThenGetStateAsync_ReturnsStoredValue()
    {
        await _repo.SetStateAsync(AppStateKey.PromptsSinceLastSuggestion, 42);
        var value = await _repo.GetStateAsync(AppStateKey.PromptsSinceLastSuggestion);
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task GetStateAsync_KeyNotSet_ReturnsZero()
    {
        var value = await _repo.GetStateAsync(AppStateKey.PromptsSinceLastSuggestion);
        Assert.Equal(0, value);
    }

    [Fact]
    public async Task SetStateAsync_UpdateExisting_OverwritesValue()
    {
        await _repo.SetStateAsync(AppStateKey.PromptsSinceLastSuggestion, 10);
        await _repo.SetStateAsync(AppStateKey.PromptsSinceLastSuggestion, 99);
        var value = await _repo.GetStateAsync(AppStateKey.PromptsSinceLastSuggestion);
        Assert.Equal(99, value);
    }

    [Fact]
    public async Task SetConfigAsync_ThenGetConfigAsync_ReturnsStoredValue()
    {
        await _repo.SetConfigAsync(AppConfigKey.UserProfile, "advanced learner");
        var value = await _repo.GetConfigAsync(AppConfigKey.UserProfile);
        Assert.Equal("advanced learner", value);
    }

    [Fact]
    public async Task GetConfigAsync_KeyNotSet_ReturnsEmptyString()
    {
        var value = await _repo.GetConfigAsync(AppConfigKey.UserProfile);
        Assert.Equal("", value);
    }

    [Fact]
    public async Task SavePromptAsync_WithBetterVersion_IsPersisted()
    {
        var payload = new HookPayload
        {
            Original = "Can you do the thing",
            Corrected = "",
            DisplayOriginal = "Can you do the thing",
            Score = 9,
            Complexity = 6,
            WordCount = 5,
            Explanations = [],
            BetterVersion = "Could you handle that?",
            HookVersion = "2.0"
        };
        await _repo.SavePromptAsync(payload);
        var prompts = await _repo.GetLastPromptsAsync(1);
        Assert.Single(prompts);
        Assert.Equal("Could you handle that?", prompts[0].BetterVersion);
    }

    [Fact]
    public async Task SavePromptAsync_WithoutBetterVersion_IsNull()
    {
        await _repo.SavePromptAsync(MakePayload(score: 8, complexity: 6, wordCount: 4));
        var prompts = await _repo.GetLastPromptsAsync(1);
        Assert.Single(prompts);
        Assert.Null(prompts[0].BetterVersion);
    }
}
