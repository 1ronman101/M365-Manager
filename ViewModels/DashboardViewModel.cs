using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using M365Manager.Services;

namespace M365Manager.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly GraphUserService _userService;
    private readonly AuthService _authService;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _totalUsers;
    [ObservableProperty] private int _licensedUsers;
    [ObservableProperty] private int _unlicensedUsers;
    [ObservableProperty] private string _welcomeMessage = "Welcome!";
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    public DashboardViewModel(
        GraphUserService userService,
        AuthService authService,
        ILogger<DashboardViewModel> logger)
    {
        _userService = userService;
        _authService = authService;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            WelcomeMessage = $"Welcome, {_authService.UserDisplayName ?? "Admin"}!";

            var (total, licensed) = await _userService.GetUserStatsAsync();
            TotalUsers = total;
            LicensedUsers = licensed;
            UnlicensedUsers = total - licensed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard data.");
            HasError = true;
            ErrorMessage = "Could not load the dashboard data. Please check your connection and try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
