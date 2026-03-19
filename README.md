# enBot

enBot is a Windows system tray application that watches your AI coding agent sessions, analyses every prompt you type for English grammar and spelling, and shows a toast notification with the corrected version, a grammar score, and explanations. Results are persisted to a local SQLite database and visualised in a built-in dashboard.

## What it does

- **Intercepts prompts** from Claude Code (via HTTP hook) or Codex (via session file watcher)
- **Analyses English quality** by sending the prompt to the `claude` or `codex` CLI and parsing a structured JSON response
- **Shows a toast notification** in the bottom-right corner with:
  - Corrected text with changed words highlighted in bold
  - Original text for comparison
  - Grammar score (1–10)
  - Linguistic complexity score (1–10)
  - Explanations of each correction
- **Skips non-English text** and bare CLI commands automatically
- **Persists all results** to `%APPDATA%/enBot/lingua.db` (SQLite)
- **Dashboard window** with charts showing score history and statistics over time

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- `claude` CLI on PATH (for Claude mode) — or `codex` CLI (for Codex mode)

## Setup

### Claude Code hook

Add the following to your Claude Code hook configuration so that every prompt is forwarded to enBot:

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "curl -s -X POST http://localhost:5151/hook -H \"Content-Type: application/json\" -d \"{\\\"original\\\": \\\"$CLAUDE_TOOL_INPUT\\\"}\" || true"
          }
        ]
      }
    ]
  }
}
```

enBot listens on `http://localhost:5151/hook`.

### Codex mode

Switch the provider to `codex` in the Settings window. enBot will watch `~/.codex/sessions/rollout-*.jsonl` automatically — no hook configuration needed.

## Build

### Debug (development)

```bash
dotnet build
```

Output: `enBot/bin/Debug/net10.0/enBot.exe`

### Release for Windows x64

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `enBot/bin/Release/net10.0/win-x64/publish/enBot.exe`

Use `--self-contained true` to bundle the .NET runtime for machines without .NET installed (larger output).

## Run

```bash
dotnet run --project enBot/enBot.csproj
```

Or run the compiled `enBot.exe` directly. The app runs as a **system tray application** with no main window — close it via the tray icon menu.

## Settings

Settings are stored at `%APPDATA%/enBot/settings.json`.

| Setting            | Values              | Description                        |
|--------------------|---------------------|------------------------------------|
| `analysisProvider` | `claude` / `codex`  | Which CLI to use for analysis      |
