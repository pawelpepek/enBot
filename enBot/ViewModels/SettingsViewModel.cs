using CommunityToolkit.Mvvm.ComponentModel;
using enBot.Models;
using enBot.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace enBot.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public bool IsClaudeAvailable { get; } = AppSettingsService.IsClaudeAvailable();
    public bool IsCodexAvailable { get; } = AppSettingsService.IsCodexAvailable();

    [ObservableProperty] private bool _isClaudeSelected;
    [ObservableProperty] private bool _isCodexSelected;
    [ObservableProperty] private bool _isClaudeMonitored;
    [ObservableProperty] private bool _isCodexMonitored;
    [ObservableProperty] private string _userProfile = "";
    [ObservableProperty] private decimal _suggestionInterval;

    private readonly AppSettingsService _appSettings;
    private readonly PromptStorageService _storageService;
    private readonly Action<bool> _onClaudeMonitoringChanged;
    private readonly Action<bool> _onCodexMonitoringChanged;

    public SettingsViewModel(
        PromptStorageService storageService,
        Action<bool> onClaudeMonitoringChanged,
        Action<bool> onCodexMonitoringChanged)
    {
        _storageService = storageService;
        _onClaudeMonitoringChanged = onClaudeMonitoringChanged;
        _onCodexMonitoringChanged = onCodexMonitoringChanged;

        _appSettings = AppSettingsService.Load();
        var provider = _appSettings.AnalysisProvider;

        // If saved provider is unavailable, fall back to whichever is available
        if (provider == AnalysisProvider.Claude && !IsClaudeAvailable && IsCodexAvailable)
            provider = AnalysisProvider.Codex;
        else if (provider == AnalysisProvider.Codex && !IsCodexAvailable && IsClaudeAvailable)
            provider = AnalysisProvider.Claude;

        _isClaudeSelected = provider == AnalysisProvider.Claude && IsClaudeAvailable;
        _isCodexSelected = provider == AnalysisProvider.Codex && IsCodexAvailable;

        _isClaudeMonitored = _appSettings.MonitorClaude;
        _isCodexMonitored = _appSettings.MonitorCodex;
        _suggestionInterval = _appSettings.SuggestionInterval;

        _ = LoadUserProfileAsync();
    }

    private async Task LoadUserProfileAsync()
    {
        UserProfile = await _storageService.GetConfigAsync(AppConfigKey.UserProfile).ConfigureAwait(false);
    }

    partial void OnIsClaudeSelectedChanged(bool value)
    {
        if (!value) return;
        _appSettings.AnalysisProvider = AnalysisProvider.Claude;
        _appSettings.Save();
    }

    partial void OnIsCodexSelectedChanged(bool value)
    {
        if (!value) return;
        _appSettings.AnalysisProvider = AnalysisProvider.Codex;
        _appSettings.Save();
    }

    partial void OnIsClaudeMonitoredChanged(bool value)
    {
        _appSettings.MonitorClaude = value;
        _appSettings.Save();
        _onClaudeMonitoringChanged(value);
        if (value)
            _ = InstallHookForUserAsync();
    }

    partial void OnIsCodexMonitoredChanged(bool value)
    {
        _appSettings.MonitorCodex = value;
        _appSettings.Save();
        _onCodexMonitoringChanged(value);
    }

    partial void OnUserProfileChanged(string value)
    {
        _ = _storageService.SetConfigAsync(AppConfigKey.UserProfile, value);
    }

    partial void OnSuggestionIntervalChanged(decimal value)
    {
        _appSettings.SuggestionInterval = (int)value;
        _appSettings.Save();
    }

    private static readonly string HookScript = """
        if (process.env.ENBOT_ANALYSIS) process.exit(0);

        let raw = "";
        for await (const chunk of process.stdin) raw += chunk;

        let data;
        try { data = JSON.parse(raw); } catch { process.exit(0); }

        const prompt = (data.prompt ?? "").trim();
        const wordCount = prompt.split(/\s+/).filter(Boolean).length;
        if (wordCount <= 1) process.exit(0);

        try {
          await fetch("http://localhost:5151/hook", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ original: prompt }),
            signal: AbortSignal.timeout(5000),
          });
        } catch { /* app not running */ }
        """;

    private static async Task InstallHookForUserAsync()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var hookPath = Path.Combine(userProfile, ".claude", "hooks", "index.js").Replace('\\', '/');
        var command = $"node \"{hookPath}\"";

        var hooksDir = Path.Combine(userProfile, ".claude", "hooks");
        Directory.CreateDirectory(hooksDir);

        await File.WriteAllTextAsync(Path.Combine(hooksDir, "index.js"), HookScript).ConfigureAwait(false);

        var settingsPath = Path.Combine(userProfile, ".claude", "settings.json");
        var settingsText = File.Exists(settingsPath)
            ? await File.ReadAllTextAsync(settingsPath).ConfigureAwait(false)
            : "{}";

        var settings = JsonNode.Parse(settingsText) as JsonObject ?? new JsonObject();

        var hookEntry = new JsonObject
        {
            ["matcher"] = "",
            ["hooks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = command
                }
            }
        };

        if (settings["hooks"] is not JsonObject hooksObj)
        {
            hooksObj = new JsonObject();
            settings["hooks"] = hooksObj;
        }

        hooksObj["UserPromptSubmit"] = new JsonArray { hookEntry };

        await File.WriteAllTextAsync(
            settingsPath,
            settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }))
            .ConfigureAwait(false);
    }
}
