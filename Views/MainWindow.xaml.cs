using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using M365Manager.Helpers;
using M365Manager.Services;
using System.Runtime.InteropServices;

namespace M365Manager.Views;

public sealed partial class MainWindow : Window
{
    private readonly AuthService _authService;

    public MainWindow()
    {
        this.InitializeComponent();

        _authService = App.Services.GetRequiredService<AuthService>();

        // Set window size and minimum size.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        appWindow.Title = "M365 Manager";

        // Set minimum window size using Win32 subclassing.
        SetWindowMinSize(hwnd, 1000, 700);
    }

    private static void SetWindowMinSize(IntPtr hwnd, int minWidth, int minHeight)
    {
        var dpi = GetDpiForWindow(hwnd);
        var scalingFactor = dpi / 96.0;
        _minWidth = (int)(minWidth * scalingFactor);
        _minHeight = (int)(minHeight * scalingFactor);

        _newWndProc = new WndProc(NewWindowProc);
        _oldWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    private static int _minWidth;
    private static int _minHeight;
    private static IntPtr _oldWndProc;
    private static WndProc? _newWndProc;

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        const uint WM_GETMINMAXINFO = 0x0024;
        if (msg == WM_GETMINMAXINFO)
        {
            var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.X = _minWidth;
            minMaxInfo.ptMinTrackSize.Y = _minHeight;
            Marshal.StructureToPtr(minMaxInfo, lParam, false);
            return IntPtr.Zero;
        }
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private const int GWLP_WNDPROC = -4;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize notification and dialog helpers.
        NotificationHelper.Initialize(NotificationPanel);

        if (!App.Settings.IsConfigured)
        {
            LoginStatusText.Text = "Please configure your Azure App Registration in Settings before signing in.";
            LoginStatusText.Visibility = Visibility.Visible;
        }
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Settings.IsConfigured)
        {
            LoginStatusText.Text = "Please open Settings and enter your Client ID and Tenant ID first.";
            LoginStatusText.Visibility = Visibility.Visible;
            return;
        }

        SignInButton.IsEnabled = false;
        LoginProgress.IsActive = true;
        LoginStatusText.Visibility = Visibility.Collapsed;

        try
        {
            if (!_authService.IsAuthenticated)
            {
                _authService.Initialize(App.Settings);
            }

            var success = await _authService.SignInAsync();

            if (success)
            {
                ShowMainApp();
            }
            else
            {
                LoginStatusText.Text = "Sign-in was cancelled. Please try again.";
                LoginStatusText.Visibility = Visibility.Visible;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            LoginStatusText.Text = ex.Message;
            LoginStatusText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            LoginStatusText.Text = $"Could not sign in: {ex.Message}";
            LoginStatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            SignInButton.IsEnabled = true;
            LoginProgress.IsActive = false;
        }
    }

    private void ShowMainApp()
    {
        LoginOverlay.Visibility = Visibility.Collapsed;
        NavView.Visibility = Visibility.Visible;

        SignedInUserText.Text = _authService.UserDisplayName ?? "";

        // Initialize DialogHelper with XamlRoot.
        DialogHelper.Initialize(Content.XamlRoot);

        // Navigate to dashboard.
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            Type? pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "CreateUser" => typeof(CreateUserPage),
                "ManageUser" => typeof(ManageUserPage),
                "Groups" => typeof(GroupsPage),
                _ => null
            };

            if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

    private async void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Sign Out",
            "Are you sure you want to sign out?",
            "Yes, sign out");

        if (confirmed)
        {
            await _authService.SignOutAsync();
            NavView.Visibility = Visibility.Collapsed;
            LoginOverlay.Visibility = Visibility.Visible;
            LoginStatusText.Visibility = Visibility.Collapsed;
        }
    }

    private void OpenSettingsFromLogin_Click(object sender, RoutedEventArgs e)
    {
        // Show the main nav temporarily to access settings.
        LoginOverlay.Visibility = Visibility.Collapsed;
        NavView.Visibility = Visibility.Visible;
        DialogHelper.Initialize(Content.XamlRoot);
        NavView.SelectedItem = NavView.SettingsItem;
        ContentFrame.Navigate(typeof(SettingsPage));
    }
}
