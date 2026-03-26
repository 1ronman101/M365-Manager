using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using M365Manager.Helpers;
using M365Manager.Models;
using M365Manager.Services;
using System.Collections.ObjectModel;

namespace M365Manager.ViewModels;

public partial class ManageUserViewModel : ObservableObject
{
    private readonly GraphUserService _userService;
    private readonly GraphGroupService _groupService;
    private readonly LicenseService _licenseService;
    private readonly ILogger<ManageUserViewModel> _logger;

    // Search
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _hasSearched;

    // Selected user
    [ObservableProperty] private AppUser? _selectedUser;
    [ObservableProperty] private bool _isUserSelected;
    [ObservableProperty] private bool _isLoadingUser;

    // Edit fields
    [ObservableProperty] private string _editJobTitle = string.Empty;
    [ObservableProperty] private string _editDepartment = string.Empty;
    [ObservableProperty] private string _editOfficeLocation = string.Empty;
    [ObservableProperty] private string _editMobilePhone = string.Empty;
    [ObservableProperty] private string _editBusinessPhone = string.Empty;

    // Password reset
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private bool _useRandomPassword = true;
    [ObservableProperty] private bool _forcePasswordChange = true;

    // License change
    [ObservableProperty] private License? _selectedNewLicense;

    // Group add
    [ObservableProperty] private GroupItem? _selectedGroupToAdd;

    // State
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // Collections
    public ObservableCollection<AppUser> SearchResults { get; } = new();
    public ObservableCollection<License> AvailableLicenses { get; } = new();
    public ObservableCollection<GroupItem> AvailableGroupsToAdd { get; } = new();

    public ManageUserViewModel(
        GraphUserService userService,
        GraphGroupService groupService,
        LicenseService licenseService,
        ILogger<ManageUserViewModel> logger)
    {
        _userService = userService;
        _groupService = groupService;
        _licenseService = licenseService;
        _logger = logger;

        NewPassword = PasswordGenerator.Generate();
    }

    [RelayCommand]
    public async Task SearchUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2) return;

        IsSearching = true;
        HasSearched = true;
        SearchResults.Clear();

        try
        {
            var results = await _userService.SearchUsersAsync(SearchQuery);
            foreach (var user in results)
                SearchResults.Add(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed.");
            NotificationHelper.ShowError("Could not search for users. Please try again.");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public async Task SelectUserAsync(AppUser user)
    {
        IsLoadingUser = true;
        IsUserSelected = false;

        try
        {
            SelectedUser = await _userService.GetUserAsync(user.Id);
            IsUserSelected = true;

            // Populate edit fields.
            EditJobTitle = SelectedUser.JobTitle;
            EditDepartment = SelectedUser.Department;
            EditOfficeLocation = SelectedUser.OfficeLocation;
            EditMobilePhone = SelectedUser.MobilePhone;
            EditBusinessPhone = SelectedUser.BusinessPhone;

            // Load available licenses.
            var licenses = await _licenseService.GetAvailableLicensesAsync();
            AvailableLicenses.Clear();
            foreach (var lic in licenses)
                AvailableLicenses.Add(lic);

            // Load groups not already a member of.
            var allGroups = await _groupService.GetGroupsForSelectionAsync();
            var memberGroupIds = SelectedUser.GroupMemberships.Select(g => g.Id).ToHashSet();
            AvailableGroupsToAdd.Clear();
            foreach (var g in allGroups.Where(g => !memberGroupIds.Contains(g.Id)))
                AvailableGroupsToAdd.Add(g);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user details.");
            NotificationHelper.ShowError("Could not load user details. Please try again.");
        }
        finally
        {
            IsLoadingUser = false;
        }
    }

    [RelayCommand]
    public async Task ResetPasswordAsync()
    {
        if (SelectedUser is null) return;

        var password = UseRandomPassword ? PasswordGenerator.Generate() : NewPassword;
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            NotificationHelper.ShowWarning("Password must be at least 8 characters.");
            return;
        }

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Reset Password",
            $"Are you sure you want to reset the password for {SelectedUser.DisplayName}?",
            "Yes, reset it");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _userService.ResetPasswordAsync(SelectedUser.Id, password, ForcePasswordChange);
            NewPassword = password;
            NotificationHelper.ShowSuccess($"Password has been reset for {SelectedUser.DisplayName}. New password: {password}");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not reset the password.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleAccountAsync()
    {
        if (SelectedUser is null) return;

        var action = SelectedUser.AccountEnabled ? "disable" : "enable";
        var confirmed = await DialogHelper.ShowConfirmationAsync(
            $"{(SelectedUser.AccountEnabled ? "Disable" : "Enable")} Account",
            $"Are you sure you want to {action} the account for {SelectedUser.DisplayName}?",
            $"Yes, {action} it");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var newState = !SelectedUser.AccountEnabled;
            await _userService.SetAccountEnabledAsync(SelectedUser.Id, newState);
            SelectedUser.AccountEnabled = newState;
            OnPropertyChanged(nameof(SelectedUser));
            NotificationHelper.ShowSuccess($"Account has been {(newState ? "enabled" : "disabled")} for {SelectedUser.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : $"Could not {action} the account.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateProfileAsync()
    {
        if (SelectedUser is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Update Profile",
            $"Save the profile changes for {SelectedUser.DisplayName}?",
            "Yes, save changes");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _userService.UpdateUserProfileAsync(
                SelectedUser.Id,
                EditJobTitle,
                EditDepartment,
                EditOfficeLocation,
                EditMobilePhone,
                EditBusinessPhone);

            // Refresh user data.
            await SelectUserAsync(SelectedUser);
            NotificationHelper.ShowSuccess("Profile updated successfully.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not update the profile.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AssignLicenseAsync()
    {
        if (SelectedUser is null || SelectedNewLicense is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Assign License",
            $"Assign {SelectedNewLicense.FriendlyName} to {SelectedUser.DisplayName}?",
            "Yes, assign it");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _licenseService.AssignLicenseAsync(SelectedUser.Id, SelectedNewLicense.SkuId);
            await SelectUserAsync(SelectedUser);
            NotificationHelper.ShowSuccess($"License assigned to {SelectedUser.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not assign the license.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RemoveLicenseAsync(LicenseDetail license)
    {
        if (SelectedUser is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Remove License",
            $"Remove {license.FriendlyName} from {SelectedUser.DisplayName}? They will lose access to the associated services.",
            "Yes, remove it");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _licenseService.RemoveLicenseAsync(SelectedUser.Id, license.SkuId);
            await SelectUserAsync(SelectedUser);
            NotificationHelper.ShowSuccess($"License removed from {SelectedUser.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not remove the license.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddToGroupAsync()
    {
        if (SelectedUser is null || SelectedGroupToAdd is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Add to Group",
            $"Add {SelectedUser.DisplayName} to {SelectedGroupToAdd.DisplayName}?",
            "Yes, add them");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _groupService.AddMemberAsync(SelectedGroupToAdd.Id, SelectedUser.Id);
            await SelectUserAsync(SelectedUser);
            NotificationHelper.ShowSuccess($"{SelectedUser.DisplayName} has been added to {SelectedGroupToAdd.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not add to the group.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RemoveFromGroupAsync(GroupItem group)
    {
        if (SelectedUser is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Remove from Group",
            $"Remove {SelectedUser.DisplayName} from {group.DisplayName}?",
            "Yes, remove them");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _groupService.RemoveMemberAsync(group.Id, SelectedUser.Id);
            await SelectUserAsync(SelectedUser);
            NotificationHelper.ShowSuccess($"{SelectedUser.DisplayName} has been removed from {group.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not remove from the group.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void GenerateRandomPassword()
    {
        NewPassword = PasswordGenerator.Generate();
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedUser = null;
        IsUserSelected = false;
    }
}
