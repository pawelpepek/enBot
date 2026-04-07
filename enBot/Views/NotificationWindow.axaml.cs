using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace enBot.Views;

public partial class NotificationWindow : Window
{
    private DispatcherTimer _timer;
    private bool _pinned;

    public NotificationWindow()
    {
        InitializeComponent();

        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null)
            closeButton.Click += OnDismiss;

        var keepOpenButton = this.FindControl<Button>("KeepOpenButton");
        if (keepOpenButton != null)
            keepOpenButton.Click += OnKeepOpen;

        SizeChanged += (_, _) => PositionBottomRight();
    }

    public void StartAutoClose(int seconds = 8)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _timer.Tick += (_, _) => { if (!_pinned) Close(); };
        _timer.Start();
    }

    private void OnKeepOpen(object sender, RoutedEventArgs e)
    {
        _pinned = true;
        _timer?.Stop();

        if (sender is Button btn)
        {
            btn.Content = "Pinned";
            btn.IsEnabled = false;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionBottomRight();
    }

    private void PositionBottomRight()
    {
        var screen = Screens.ScreenFromVisual(this) ?? Screens.Primary;
        if (screen is null) return;

        var workArea = screen.WorkingArea;
        var scaling = screen.Scaling;

        // Bounds are in logical pixels; workArea is in physical pixels — multiply by scaling
        var w = (int)((Bounds.Width > 0 ? Bounds.Width : Width) * scaling);
        var h = (int)((Bounds.Height > 0 ? Bounds.Height : 200) * scaling);

        Position = new PixelPoint(
            workArea.Right - w - 20,
            workArea.Bottom - h - 20
        );
    }

    private void OnDismiss(object sender, RoutedEventArgs e)
    {
        _timer?.Stop();
        Close();
    }
}
