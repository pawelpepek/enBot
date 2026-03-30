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
    private record CodexEvent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("payload")] CodexPayload Payload);

    private record CodexPayload(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("message")] string Message);

    private record SessionMetaLine(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("payload")] SessionMetaPayload Payload);

    private record SessionMetaPayload(
        [property: JsonPropertyName("originator")] string Originator);

    public Action<RawPrompt> OnRawPromptReceived { get; set; }

    private CancellationTokenSource _cts;
    private readonly Dictionary<string, long> _filePositions = new();
    private readonly HashSet<string> _ownExecSessions = new();
    // Files whose session_meta wasn't readable yet; value = retry count
    private readonly Dictionary<string, int> _pendingNewFiles = new();
    private readonly string _sessionsRoot;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private const int MaxPendingRetries = 20; // 10 s at 500 ms/poll

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

    public void Dispose() => Stop();

    private void PollLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                foreach (var file in Directory.GetFiles(_sessionsRoot, "rollout-*.jsonl", SearchOption.AllDirectories))
                    ReadNewLines(file);
            }
            catch (Exception ex)
            {
                LogService.Log("[Codex] PollLoop exception", ex);
            }

            token.WaitHandle.WaitOne(PollInterval);
        }
    }

    private void ReadNewLines(string path)
    {
        if (IsBlacklistedExecSession(path)) return;

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (!_filePositions.TryGetValue(path, out var startPos))
            {
                var isExec = TryIsCodexExecSession(fs);

                if (isExec == null)
                {
                    if (DeferUntilSessionMetaIsReady(path)) return;
                    LogService.Log($"[Codex] Gave up waiting for session_meta, treating as user session: {Path.GetFileName(path)}");
                }

                _pendingNewFiles.Remove(path);

                if (isExec == true)
                {
                    BlacklistExecSession(path);
                    return;
                }

                SeekToBeginningOfUserSession(fs, out startPos);
            }

            if (fs.Length <= startPos) return;

            fs.Seek(startPos, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            string line;
            while ((line = reader.ReadLine()) is not null)
            {
                var message = TryParseUserMessage(line);
                if (message is not null)
                {
                    LogService.Log($"[Codex] User message detected in {Path.GetFileName(path)}");
                    OnRawPromptReceived?.Invoke(new RawPrompt { Original = message });
                }
            }
            _filePositions[path] = fs.Position;
        }
        catch (Exception ex)
        {
            LogService.Log($"[Codex] ReadNewLines exception for {Path.GetFileName(path)}", ex);
        }
    }

    private bool IsBlacklistedExecSession(string path) =>
        _ownExecSessions.Contains(path);

    private bool DeferUntilSessionMetaIsReady(string path)
    {
        _pendingNewFiles.TryGetValue(path, out var retries);
        if (retries >= MaxPendingRetries) return false;
        _pendingNewFiles[path] = retries + 1;
        LogService.Log($"[Codex] New file, session_meta not ready yet (retry {retries + 1}): {Path.GetFileName(path)}");
        return true;
    }

    private void BlacklistExecSession(string path)
    {
        _ownExecSessions.Add(path);
        LogService.Log($"[Codex] Blacklisted own exec session: {Path.GetFileName(path)}");
    }

    private static void SeekToBeginningOfUserSession(FileStream fs, out long startPos)
    {
        fs.Seek(0, SeekOrigin.Begin);
        startPos = 0;
    }

    // Returns true = enBot's exec session (skip), false = user session (process), null = not readable yet (defer)
    private static bool? TryIsCodexExecSession(FileStream fs)
    {
        try
        {
            using var reader = new StreamReader(fs, leaveOpen: true);
            var firstLine = reader.ReadLine();
            if (string.IsNullOrEmpty(firstLine)) return null;
            var meta = JsonSerializer.Deserialize<SessionMetaLine>(firstLine);
            if (meta?.Type != "session_meta") return null;
            return meta.Payload?.Originator == "codex_exec";
        }
        catch { return null; }
    }

    private static readonly string[] CodexSystemPrefixes =
    [
        "PLEASE IMPLEMENT THIS PLAN:",
    ];

    private static string TryParseUserMessage(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        try
        {
            var ev = JsonSerializer.Deserialize<CodexEvent>(line);
            if (ev?.Type == "event_msg" &&
                ev.Payload?.Type == "user_message" &&
                !string.IsNullOrWhiteSpace(ev.Payload.Message) &&
                !IsCodexSystemMessage(ev.Payload.Message))
                return ev.Payload.Message;
        }
        catch { }
        return null;
    }

    private static bool IsCodexSystemMessage(string message) =>
        Array.Exists(CodexSystemPrefixes, p => message.StartsWith(p, StringComparison.OrdinalIgnoreCase));
}
