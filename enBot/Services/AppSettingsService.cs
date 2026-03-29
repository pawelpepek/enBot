using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using enBot.Models;

namespace enBot.Services;

public class AppSettingsService
{
    [JsonPropertyName("analysisProvider")]
    public AnalysisProvider AnalysisProvider { get; set; } = AnalysisProvider.Claude;

    [JsonPropertyName("monitorClaude")]
    public bool MonitorClaude { get; set; } = false;

    [JsonPropertyName("monitorCodex")]
    public bool MonitorCodex { get; set; } = false;

    [JsonPropertyName("userProfile")]
    public string UserProfile { get; set; } = "";

    [JsonPropertyName("suggestionInterval")]
    public int SuggestionInterval { get; set; } = 10;

    private static string SettingsFilePath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "enBot", "settings.json");
        }
    }

    public static AppSettingsService Load()
    {
        try
        {
            var path = SettingsFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettingsService>(json) ?? new AppSettingsService();
            }
        }
        catch { }
        return new AppSettingsService();
    }

    public void Save()
    {
        try
        {
            var path = SettingsFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public static bool IsClaudeAvailable() => IsCliAvailable("claude");
    public static bool IsCodexAvailable() => IsCliAvailable("codex");

    private static bool IsCliAvailable(string exe)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("cmd.exe", $"/c {exe} --version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            p?.WaitForExit(2000);
            return true;
        }
        catch { return false; }
    }
}
