using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace M365Manager.Helpers;

/// <summary>
/// Provides simple in-app toast-style notifications using InfoBar.
/// </summary>
public static class NotificationHelper
{
    private static Panel? _container;

    public static void Initialize(Panel container)
    {
        _container = container;
    }

    public static void ShowSuccess(string message, string title = "Success")
    {
        Show(title, message, InfoBarSeverity.Success);
    }

    public static void ShowError(string message, string title = "Something went wrong")
    {
        Show(title, message, InfoBarSeverity.Error);
    }

    public static void ShowWarning(string message, string title = "Warning")
    {
        Show(title, message, InfoBarSeverity.Warning);
    }

    public static void ShowInfo(string message, string title = "Info")
    {
        Show(title, message, InfoBarSeverity.Informational);
    }

    private static void Show(string title, string message, InfoBarSeverity severity)
    {
        if (_container is null) return;

        _container.DispatcherQueue.TryEnqueue(() =>
        {
            var infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
                IsClosable = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4)
            };

            infoBar.Closed += (s, e) =>
            {
                _container.Children.Remove(infoBar);
            };

            _container.Children.Add(infoBar);

            // Auto-dismiss after 5 seconds.
            var timer = _container.DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.IsRepeating = false;
            timer.Tick += (s, e) =>
            {
                infoBar.IsOpen = false;
                timer.Stop();
            };
            timer.Start();
        });
    }
}
