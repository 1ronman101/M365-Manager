using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using M365Manager.Models;
using M365Manager.ViewModels;

namespace M365Manager.Views;

public sealed partial class ManageUserPage : Page
{
    public ManageUserViewModel ViewModel { get; }

    public ManageUserPage()
    {
        ViewModel = App.Services.GetRequiredService<ManageUserViewModel>();
        this.InitializeComponent();
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SearchUsersAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.SearchUsersAsync();
        }
    }

    private async void ViewUserDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AppUser user)
        {
            await ViewModel.SelectUserAsync(user);
        }
    }

    private void BackToSearch_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearSelectionCommand.Execute(null);
    }

    private async void UpdateProfile_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateProfileAsync();
    }

    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ResetPasswordAsync();
    }

    private void GenerateRandomPwd_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateRandomPasswordCommand.Execute(null);
    }

    private async void ToggleAccount_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleAccountAsync();
    }

    private async void AssignLicense_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AssignLicenseAsync();
    }

    private async void RemoveLicense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LicenseDetail license)
        {
            await ViewModel.RemoveLicenseAsync(license);
        }
    }

    private async void AddToGroup_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddToGroupAsync();
    }

    private async void RemoveFromGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Models.GroupItem group)
        {
            await ViewModel.RemoveFromGroupAsync(group);
        }
    }
}
