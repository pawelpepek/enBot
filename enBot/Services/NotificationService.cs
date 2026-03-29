using System;
using Avalonia.Threading;
using enBot.Models;
using enBot.ViewModels;
using enBot.Views;

namespace enBot.Services;

public class NotificationService
{
    private readonly int _autoCloseSeconds;

    public NotificationService(int autoCloseSeconds = 8)
    {
        _autoCloseSeconds = autoCloseSeconds;
    }

    private NotificationWindow _current;
    private SuggestionNotificationWindow _currentSuggestion;

    public void ShowSuggestion(string suggestionText, string explanationText, int autoCloseSeconds)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _currentSuggestion?.Close();
                _currentSuggestion = new SuggestionNotificationWindow();
                _currentSuggestion.SetContent(suggestionText, explanationText);
                _currentSuggestion.Closed += (_, _) => _currentSuggestion = null;
                _currentSuggestion.Show();
                _currentSuggestion.StartAutoClose(autoCloseSeconds);
                LogService.Log("[Notification] Suggestion window shown");
            }
            catch (Exception ex)
            {
                LogService.Log("[Notification] Failed to show suggestion window", ex);
            }
        });
    }

    public void Show(HookPayload payload)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _current?.Close();
                var vm = new NotificationViewModel(payload);
                _current = new NotificationWindow { DataContext = vm };
                _current.Closed += (_, _) => _current = null;
                _current.Show();
                _current.StartAutoClose(_autoCloseSeconds);
                LogService.Log("[Notification] Window shown");
            }
            catch (Exception ex)
            {
                LogService.Log("[Notification] Failed to show window", ex);
            }
        });
    }
}
