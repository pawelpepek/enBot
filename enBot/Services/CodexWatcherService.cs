using enBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace enBot.Services;

public class CodexWatcherService : IDisposable
{
    public Action<RawPrompt>? OnRawPromptReceived { get; set; }

    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, long> _filePositions = new();
    private readonly string _sessionsRoot;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    public CodexWatcherService()
    {
        _sessionsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex", "sessions");
    }

    public void Start()
    {
        if (!Directory.Exists(_sessionsRoot))
            return;

        // Seed positions to current EOF for all existing files (skip history)
        foreach (var file in Directory.GetFiles(_sessionsRoot, "rollout-*.jsonl", SearchOption.AllDirectories))
        {
            try { _filePositions[file] = new FileInfo(file).Length; }
            catch { }
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        Thread thread = new(() => PollLoop(token)) { IsBackground = true, Name = "CodexWatcher" };
        thread.Start();
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void PollLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                foreach (var file in Directory.GetFiles(_sessionsRoot, "rollout-*.jsonl", SearchOption.AllDirectories))
                    ReadNewLines(file);
            }
            catch { }

            token.WaitHandle.WaitOne(PollInterval);
        }
    }

    private void ReadNewLines(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var startPos = _filePositions.GetValueOrDefault(path, 0);
            if (fs.Length <= startPos) return;

            fs.Seek(startPos, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var message = TryParseUserMessage(line);
                if (message is not null)
                    OnRawPromptReceived?.Invoke(new RawPrompt { Original = message });
            }
            _filePositions[path] = fs.Position;
        }
        catch { }
    }

    private static string? TryParseUserMessage(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        try
        {
            var ev = JsonSerializer.Deserialize<CodexEvent>(line);
            if (ev?.Type == "event_msg" &&
                ev.Payload?.Type == "user_message" &&
                !string.IsNullOrWhiteSpace(ev.Payload.Message))
                return ev.Payload.Message;
        }
        catch { }
        return null;
    }

    public void Dispose() => Stop();

    private record CodexEvent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("payload")] CodexPayload? Payload);

    private record CodexPayload(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("message")] string Message);
}
