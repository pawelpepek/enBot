using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using enBot.Models;
using enBot.Services;
using enBot.ViewModels;
using Microsoft.EntityFrameworkCore;
using enBot.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace enBot;

public partial class App : Application
{
    private HttpListenerService _httpListenerService;
    private CodexWatcherService _codexWatcherService;
    private NotificationService _notificationService;
    private PromptStorageService _storageService;
    private IAnalysisService _analysisService;
    private TrayIcon _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
        DisableAvaloniaDataAnnotationValidation();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Don't show a main window — run as tray app
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize DB
            var dbPath = GetDatabasePath();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            _storageService = new PromptStorageService(options);
            _ = _storageService.InitializeAsync();

            // Services
            _notificationService = new NotificationService(autoCloseSeconds: 30);
            var settings = AppSettingsService.Load();
            var processor = AgentCliProcessorFactory.Create(settings.AnalysisProvider);
            _analysisService = new AnalysisService(processor);

            _httpListenerService = new HttpListenerService("http://localhost:5151/");
            _httpListenerService.OnRawPromptReceived = HandleRawPrompt;

            _codexWatcherService = new CodexWatcherService();
            _codexWatcherService.OnRawPromptReceived = HandleRawPrompt;

            // Tray icon
            var trayViewModel = new TrayViewModel(
                _storageService,
                v => { if (v) _httpListenerService.Start(); else _httpListenerService.Stop(); },
                v => { if (v) _codexWatcherService.Start(); else _codexWatcherService.Stop(); });
            SetupTrayIcon(trayViewModel);

            // Start services
            desktop.Startup += (_, _) =>
            {
                if (settings.MonitorClaude) _httpListenerService.Start();
                if (settings.MonitorCodex) _codexWatcherService.Start();
            };
            desktop.Exit += (_, _) => { _httpListenerService.Stop(); _codexWatcherService.Stop(); };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void HandleRawPrompt(RawPrompt rawPrompt)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = await _analysisService!.AnalyzeAsync(rawPrompt.Original).ConfigureAwait(false);
                if (payload is null) return;
                _notificationService!.Show(payload);
                await _storageService!.SavePromptAsync(payload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(Path.GetTempPath(), "enBot_storage_error.log");
                await File.AppendAllTextAsync(logPath, $"[{DateTime.UtcNow:O}] {ex}\n\n").ConfigureAwait(false);
            }
        });
    }

    private void SetupTrayIcon(TrayViewModel trayViewModel)
    {
        var menu = new NativeMenu();

        var dashboardItem = new NativeMenuItem("Open Dashboard");
        dashboardItem.Click += (_, _) => trayViewModel.OpenDashboard();
        menu.Add(dashboardItem);

        var settingsItem = new NativeMenuItem("Settings");
        settingsItem.Click += (_, _) => trayViewModel.OpenSettings();
        menu.Add(settingsItem);

        menu.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) => trayViewModel.Exit();
        menu.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "enBot",
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://enBot/Assets/avalonia-logo.png"))),
            Menu = menu,
            IsVisible = true
        };

        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private static string GetDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "enBot");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "lingua.db");
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}
