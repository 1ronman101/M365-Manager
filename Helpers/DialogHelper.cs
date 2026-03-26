using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace M365Manager.Helpers;

public static class DialogHelper
{
    private static XamlRoot? _xamlRoot;

    public static void Initialize(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    public static async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmText = "Yes, go ahead",
        string cancelText = "Cancel")
    {
        if (_xamlRoot is null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public static async Task ShowMessageAsync(string title, string message)
    {
        if (_xamlRoot is null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = _xamlRoot
        };

        await dialog.ShowAsync();
    }

    public static async Task ShowErrorAsync(string message)
    {
        await ShowMessageAsync("Something went wrong", message);
    }
}
