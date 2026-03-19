# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Response Style

When referencing a CLI command or slash command sent by the user, do not repeat the full command text. Instead, display only ***command*** (bold and italic).

## Build & Run

```bash
dotnet build
dotnet run --project enBot/enBot.csproj
```

The compiled binary is output to `enBot/bin/Debug/net10.0/enBot.exe`.

The app runs as a **system tray application** with no main window — `ShutdownMode` is set to `OnExplicitShutdown`. Close via the tray icon menu.

## Architecture

enBot is an Avalonia desktop tray app (.NET 10) following MVVM architecture via **CommunityToolkit.Mvvm**. Its purpose is to intercept prompts from AI coding agents (Claude Code or Codex), analyse them for English quality using the selected CLI, show a toast notification with corrections and score, and persist results for dashboard visualisation.

### Analysis providers

The active provider is stored in `AppSettingsService` (`%APPDATA%/enBot/settings.json`, field `analysisProvider`: `"claude"` or `"codex"`). `AgentCliProcessorFactory.Create(provider)` returns the appropriate `IAgentCliProcessor` implementation (`ClaudeCliProcessor` or `CodexCliProcessor`). `AnalysisService` uses that processor to spawn the CLI and parse the JSON response.

### Request flow — Claude mode

1. **Claude Code hook** POSTs JSON `{"original": "..."}` to `http://localhost:5151/hook`
2. `HttpListenerService` receives it and fires `OnRawPromptReceived`
3. `AnalysisService.AnalyzeAsync` spawns `claude --print` via `ClaudeCliProcessor`, passes the analysis prompt, and parses the JSON response
4. `NotificationService` shows a `NotificationWindow` (auto-closes after 8 s) on the UI thread via `Dispatcher.UIThread.Post`
5. `PromptStorageService` saves a `PromptEntry` to SQLite at `%APPDATA%/enBot/lingua.db`

### Request flow — Codex mode

1. `CodexWatcherService` polls `~/.codex/sessions/rollout-*.jsonl` every 500 ms for new lines
2. Lines with `type=event_msg` and `payload.type=user_message` are extracted and fire `OnRawPromptReceived`
3. Steps 3–5 are identical to Claude mode, using `CodexCliProcessor` instead

### Key conventions

- `ViewLocator.cs` auto-discovers Views from ViewModels by convention: replaces `"ViewModel"` suffix with `"View"` in the type name.
- ViewModels inherit from `ViewModelBase` (which extends `CommunityToolkit.Mvvm.ObservableObject`).
- Bindings use compiled bindings (`AvaloniaUseCompiledBindingsByDefault=true`) — binding paths are resolved at compile time.
- `ClaudeCliProcessor` sets env var `ENBOT_ANALYSIS=1` when spawning the `claude` subprocess so hooks can detect and skip recursive invocations.
- `PromptStorageService` creates a new `AppDbContext` per operation (no shared context); DB is initialised via `EnsureCreatedAsync` (no migrations).
- `Explanations` are stored in `PromptEntry.ExplanationsJson` as a serialised JSON string.
- Inputs with fewer than 2 words are skipped entirely before hitting the AI (`AnalysisService`).
- Non-English text returns `{"language": "--"}` from the AI and is silently ignored.

### Layer responsibilities

- `Views/` — AXAML UI and code-behind (MainWindow, DashboardWindow, NotificationWindow, SettingsWindow)
- `ViewModels/` — presentation logic; `DashboardViewModel` builds LiveCharts series from `PromptStorageService`
- `Models/` — `AnalysisResult` (raw AI response), `HookPayload` (parsed analysis result), `PromptEntry` (EF Core entity), `RawPrompt`, `InlineSegment`
- `Services/` — `HttpListenerService`, `CodexWatcherService`, `AnalysisService`, `NotificationService`, `PromptStorageService`, `AppSettingsService`, `AgentCliProcessorFactory`, `ClaudeCliProcessor`, `CodexCliProcessor`
- `Data/` — `AppDbContext`, `AppDbContextFactory`
- `Converters/` — value converters for score/complexity colours and bold/italic formatting

## Tech Stack

- **Avalonia 11.3.12** — cross-platform XAML UI framework
- **CommunityToolkit.Mvvm 8.2.1** — MVVM helpers (`ObservableObject`, `[RelayCommand]`, `[ObservableProperty]`, etc.)
- **EF Core 9 + SQLite** — persistence via `Microsoft.EntityFrameworkCore.Sqlite`
- **LiveChartsCore.SkiaSharpView.Avalonia 2.0.0-rc6.1** — charts in DashboardWindow
- **Fluent theme** with system theme variant auto-detection
- **Nullable reference types** enabled
- **Runtime dependency**: `claude` or `codex` CLI must be on PATH depending on the selected provider
