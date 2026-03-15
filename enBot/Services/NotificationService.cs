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

    private NotificationWindow? _current;

    public void Show(HookPayload payload)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _current?.Close();
            var vm = new NotificationViewModel(payload);
            _current = new NotificationWindow { DataContext = vm };
            _current.Closed += (_, _) => _current = null;
            _current.Show();
            _current.StartAutoClose(_autoCloseSeconds);
        });
    }
}
