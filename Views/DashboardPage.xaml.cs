using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using M365Manager.ViewModels;

namespace M365Manager.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
        this.InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
    }

    private void NavigateCreateUser_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = App.MainWindow as MainWindow;
        // Navigate by finding the frame and navigating directly.
        if (this.Frame is Frame frame)
        {
            frame.Navigate(typeof(CreateUserPage));
        }
    }

    private void NavigateManageUser_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame is Frame frame)
        {
            frame.Navigate(typeof(ManageUserPage));
        }
    }

    private async void RefreshData_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
    }
}
