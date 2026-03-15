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

enBot is an Avalonia desktop tray app (.NET 10) following MVVM architecture via **CommunityToolkit.Mvvm**. Its purpose is to intercept Claude Code prompts via a local HTTP hook, analyze them for English quality using the `claude` CLI, show a toast notification, and persist results for dashboard visualisation.

### Request flow

1. **Claude Code hook** POSTs JSON `{"original": "..."}` to `http://localhost:5151/hook`
2. `HttpListenerService` receives it and fires `OnRawPromptReceived`
3. `ClaudeAnalysisService.AnalyzeAsync` spawns `claude --print` as a subprocess, passes the analysis prompt via stdin, and parses the JSON response
4. `NotificationService` shows a `NotificationWindow` (auto-closes after 30 s) on the UI thread via `Dispatcher.UIThread.Post`
5. `PromptStorageService` saves a `PromptEntry` to SQLite at `%APPDATA%/enBot/lingua.db`

### Key conventions

- `ViewLocator.cs` auto-discovers Views from ViewModels by convention: replaces `"ViewModel"` suffix with `"View"` in the type name.
- ViewModels inherit from `ViewModelBase` (which extends `CommunityToolkit.Mvvm.ObservableObject`).
- Bindings use compiled bindings (`AvaloniaUseCompiledBindingsByDefault=true`) — binding paths are resolved at compile time.
- `ClaudeAnalysisService` sets env var `ENBOT_ANALYSIS=1` when spawning the `claude` subprocess so hooks can detect and skip recursive invocations.
- `PromptStorageService` creates a new `AppDbContext` per operation (no shared context); DB is initialised via `EnsureCreatedAsync` (no migrations).
- `Explanations` are stored in `PromptEntry.ExplanationsJson` as a serialised JSON string.

### Layer responsibilities

- `Views/` — AXAML UI and code-behind (MainWindow, DashboardWindow, NotificationWindow, SettingsWindow)
- `ViewModels/` — presentation logic; `DashboardViewModel` builds LiveCharts series from `PromptStorageService`
- `Models/` — `HookPayload` (analysis result record), `PromptEntry` (EF Core entity), `RawPrompt`, `InlineSegment`
- `Services/` — `HttpListenerService`, `ClaudeAnalysisService`, `NotificationService`, `PromptStorageService`
- `Data/` — `AppDbContext`, `AppDbContextFactory`
- `Converters/` — value converters for score/complexity colours and bold/italic formatting

## Tech Stack

- **Avalonia 11.3.12** — cross-platform XAML UI framework
- **CommunityToolkit.Mvvm 8.2.1** — MVVM helpers (`ObservableObject`, `[RelayCommand]`, `[ObservableProperty]`, etc.)
- **EF Core 9 + SQLite** — persistence via `Microsoft.EntityFrameworkCore.Sqlite`
- **LiveChartsCore.SkiaSharpView.Avalonia 2.0.0-rc6.1** — charts in DashboardWindow
- **Fluent theme** with system theme variant auto-detection
- **Nullable reference types** enabled
- **Runtime dependency**: `claude` CLI must be on PATH for analysis to work
