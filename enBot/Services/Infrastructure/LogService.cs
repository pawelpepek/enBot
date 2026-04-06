using System;
using System.IO;

namespace enBot.Services.Infrastructure;

public static class LogService
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "enBot", "enBot.log");

    private static readonly object _lock = new();
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                var info = new FileInfo(LogPath);
                if (info.Exists && info.Length > MaxBytes)
                    File.Move(LogPath, LogPath + ".old", overwrite: true);

                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }

    public static void Log(string message, Exception ex) =>
        Log($"{message} — {ex.GetType().Name}: {ex.Message}");
}
