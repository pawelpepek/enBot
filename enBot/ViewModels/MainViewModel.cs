using enBot.Services;
using System;

namespace enBot.ViewModels;

public class MainViewModel : ViewModelBase
{
    public DashboardViewModel Dashboard { get; }
    public PromptSuggestionsViewModel Suggestions { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(
        PromptStorageService storageService,
        PromptSuggestionService suggestionService,
        Action<bool> onClaudeMonitoringChanged,
        Action<bool> onCodexMonitoringChanged)
    {
        Dashboard = new DashboardViewModel(storageService);
        Suggestions = new PromptSuggestionsViewModel(suggestionService);
        Settings = new SettingsViewModel(onClaudeMonitoringChanged, onCodexMonitoringChanged);
    }
}
