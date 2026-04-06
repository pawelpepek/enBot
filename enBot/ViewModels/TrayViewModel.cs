using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using enBot.Services.Analysis;
using enBot.Services.Infrastructure;
using enBot.Views;
using System;

namespace enBot.ViewModels;

public class TrayViewModel
{
    private readonly AppRepository _storageService;
    private readonly PromptSuggestionService _suggestionService;
    private readonly ReportService _reportService;
    private readonly Action<bool> _onClaudeMonitoringChanged;
    private readonly Action<bool> _onCodexMonitoringChanged;

    public TrayViewModel(
        AppRepository storageService,
        PromptSuggestionService suggestionService,
        ReportService reportService,
        Action<bool> onClaudeMonitoringChanged,
        Action<bool> onCodexMonitoringChanged)
    {
        _storageService = storageService;
        _suggestionService = suggestionService;
        _reportService = reportService;
        _onClaudeMonitoringChanged = onClaudeMonitoringChanged;
        _onCodexMonitoringChanged = onCodexMonitoringChanged;
    }

    private MainWindow _mainWindow;

    public void OpenMain()
    {
        if (_mainWindow != null)
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Topmost = true;
            _mainWindow.Activate();
            _mainWindow.Topmost = false;
            return;
        }

        var vm = new MainViewModel(_storageService, _suggestionService, _reportService, _onClaudeMonitoringChanged, _onCodexMonitoringChanged);
        _mainWindow = new MainWindow { DataContext = vm };
        _mainWindow.Closed += (_, _) => _mainWindow = null;
        _mainWindow.Show();
    }

    public void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }
}
