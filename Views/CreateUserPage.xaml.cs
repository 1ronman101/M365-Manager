using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using M365Manager.Helpers;
using M365Manager.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace M365Manager.Views;

public sealed partial class CreateUserPage : Page
{
    public CreateUserViewModel ViewModel { get; }

    public CreateUserPage()
    {
        ViewModel = App.Services.GetRequiredService<CreateUserViewModel>();
        this.InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
    }

    private void GeneratePassword_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateNewPasswordCommand.Execute(null);
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Validate())
        {
            NotificationHelper.ShowWarning("Please fill in all required fields.");
            return;
        }

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Create New Employee",
            $"Create a new account for {ViewModel.DisplayName} ({ViewModel.Email})?\n\nThis will set up their Microsoft 365 account immediately.",
            "Yes, create account");

        if (confirmed)
        {
            await ViewModel.CreateUserAsync();
        }
    }

    private void CopyWelcomeInfo_Click(object sender, RoutedEventArgs e)
    {
        var text = ViewModel.GetWelcomeClipboardText();
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
        NotificationHelper.ShowSuccess("Welcome info copied to clipboard! You can now paste it into an email.");
    }

    private void ResetForm_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetFormCommand.Execute(null);
    }
}
