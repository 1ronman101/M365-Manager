using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using M365Manager.ViewModels;

namespace M365Manager.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        this.InitializeComponent();
        this.Resources["TestResultTitle"] = new TestResultTitleConverter();
        this.Resources["TestResultSeverity"] = new TestResultSeverityConverter();
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveSettingsAsync();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.TestConnectionAsync();
    }
}

public class TestResultTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? "Connection Successful" : "Connection Failed";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class TestResultSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? InfoBarSeverity.Success : InfoBarSeverity.Error;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
