using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using M365Manager.Helpers;
using M365Manager.Services;

namespace M365Manager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ILogger<SettingsViewModel> _logger;
    private AppSettings _settings;

    [ObservableProperty] private string _clientId = string.Empty;
    [ObservableProperty] private string _tenantId = string.Empty;
    [ObservableProperty] private string _redirectUri = "http://localhost";
    [ObservableProperty] private string _appVersion = "1.0.0";
    [ObservableProperty] private bool _isTesting;
    [ObservableProperty] private string _testResult = string.Empty;
    [ObservableProperty] private bool _testSuccess;
    [ObservableProperty] private bool _hasTestResult;
    [ObservableProperty] private bool _isSaving;

    public SettingsViewModel(AuthService authService, ILogger<SettingsViewModel> logger)
    {
        _authService = authService;
        _logger = logger;
        _settings = AppSettings.Load();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        ClientId = _settings.ClientId;
        TenantId = _settings.TenantId;
        RedirectUri = _settings.RedirectUri;
        AppVersion = _settings.Version;
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        IsSaving = true;

        try
        {
            _settings.ClientId = ClientId.Trim();
            _settings.TenantId = TenantId.Trim();
            _settings.RedirectUri = RedirectUri.Trim();
            _settings.Save();

            // Reinitialize auth with new settings.
            _authService.Initialize(_settings);

            NotificationHelper.ShowSuccess("Settings saved successfully. You may need to sign in again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            NotificationHelper.ShowError("Could not save settings. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        IsTesting = true;
        HasTestResult = false;

        try
        {
            // Save first in case settings changed.
            _settings.ClientId = ClientId.Trim();
            _settings.TenantId = TenantId.Trim();
            _settings.RedirectUri = RedirectUri.Trim();
            _settings.Save();

            _authService.Initialize(_settings);
            var signedIn = await _authService.SignInAsync();

            if (signedIn)
            {
                var (success, message) = await _authService.TestConnectionAsync();
                TestSuccess = success;
                TestResult = message;
            }
            else
            {
                TestSuccess = false;
                TestResult = "Sign-in was cancelled or failed.";
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            TestSuccess = false;
            TestResult = ex.Message;
        }
        catch (Exception ex)
        {
            TestSuccess = false;
            TestResult = $"Connection test failed: {ex.Message}";
            _logger.LogError(ex, "Connection test failed.");
        }
        finally
        {
            IsTesting = false;
            HasTestResult = true;
        }
    }
}
