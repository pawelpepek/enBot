using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [ObservableProperty] private string _selectedFolder = "";
    [ObservableProperty] private string _statusMessage = "";
    public bool IsClaudeAvailable { get; } = AppSettingsService.IsClaudeAvailable();
    public bool IsCodexAvailable { get; } = AppSettingsService.IsCodexAvailable();

    [ObservableProperty] private bool _isClaudeSelected;
    [ObservableProperty] private bool _isCodexSelected;

    private readonly AppSettingsService _appSettings;

    public SettingsViewModel()
    {
        _appSettings = AppSettingsService.Load();
        var provider = _appSettings.AnalysisProvider;

        // If saved provider is unavailable, fall back to whichever is available
        if (provider == AnalysisProvider.Claude && !IsClaudeAvailable && IsCodexAvailable)
            provider = AnalysisProvider.Codex;
        else if (provider == AnalysisProvider.Codex && !IsCodexAvailable && IsClaudeAvailable)
            provider = AnalysisProvider.Claude;

        _isClaudeSelected = provider == AnalysisProvider.Claude && IsClaudeAvailable;
        _isCodexSelected = provider == AnalysisProvider.Codex && IsCodexAvailable;
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

    private const string HookScript = """
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

    [RelayCommand]
    private async Task InstallHook()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            StatusMessage = "Please select a project folder first.";
            return;
        }

        var command = "node .claude/hooks/index.js";
        await InstallAsync(SelectedFolder, command, withPermissionsDeny: true).ConfigureAwait(false);
        StatusMessage = $"Hook installed in {SelectedFolder}";
    }

    [RelayCommand]
    private async Task InstallForUser()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var hookPath = Path.Combine(userProfile, ".claude", "hooks", "index.js").Replace('\\', '/');
        var command = $"node \"{hookPath}\"";
        await InstallAsync(userProfile, command, withPermissionsDeny: false).ConfigureAwait(false);
        StatusMessage = $"Hook installed for current user ({userProfile})";
    }

    private async Task InstallAsync(string targetFolder, string command, bool withPermissionsDeny)
    {
        var hooksDir = Path.Combine(targetFolder, ".claude", "hooks");
        Directory.CreateDirectory(hooksDir);

        var indexJsPath = Path.Combine(hooksDir, "index.js");
        await File.WriteAllTextAsync(indexJsPath, HookScript).ConfigureAwait(false);

        var settingsPath = Path.Combine(targetFolder, ".claude", "settings.json");
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

        if (withPermissionsDeny)
        {
            if (settings["permissions"] is not JsonObject permObj)
            {
                permObj = new JsonObject();
                settings["permissions"] = permObj;
            }

            permObj["deny"] = new JsonArray
            {
                "Bash(*)", "Edit(*)", "Write(*)", "MultiEdit(*)", "NotebookEdit(*)"
            };
        }

        var json = settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, json).ConfigureAwait(false);
    }
}
