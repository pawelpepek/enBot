using Avalonia.Controls.ApplicationLifetimes;
using enBot.Services;
using enBot.Views;

namespace enBot.ViewModels;

public class TrayViewModel
{
    private readonly PromptStorageService _storageService;

    public TrayViewModel(PromptStorageService storageService)
    {
        _storageService = storageService;
    }

    private DashboardWindow? _dashboardWindow;

    public void OpenDashboard()
    {
        if (_dashboardWindow != null)
        {
            if (_dashboardWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                _dashboardWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            _dashboardWindow.Topmost = true;
            _dashboardWindow.Activate();
            _dashboardWindow.Topmost = false;
            return;
        }

        var vm = new DashboardViewModel(_storageService);
        _dashboardWindow = new DashboardWindow { DataContext = vm };
        _dashboardWindow.Closed += (_, _) => _dashboardWindow = null;
        _dashboardWindow.Show();
    }

    public void OpenSettings()
    {
        var vm = new SettingsViewModel();
        var window = new SettingsWindow { DataContext = vm };
        window.Show();
    }

    public void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }
}
