using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using M365Manager.Helpers;
using M365Manager.Models;
using M365Manager.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace M365Manager.ViewModels;

public partial class CreateUserViewModel : ObservableObject
{
    private readonly GraphUserService _userService;
    private readonly GraphGroupService _groupService;
    private readonly LicenseService _licenseService;
    private readonly ILogger<CreateUserViewModel> _logger;

    // Form fields
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _jobTitle = string.Empty;
    [ObservableProperty] private string _department = string.Empty;
    [ObservableProperty] private string _officeLocation = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _forceChangePassword = true;

    // Selections
    [ObservableProperty] private License? _selectedLicense;
    [ObservableProperty] private AzureRole? _selectedRole;

    // State
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private bool _showSummary;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _domain = string.Empty;

    // Validation
    [ObservableProperty] private string _firstNameError = string.Empty;
    [ObservableProperty] private string _lastNameError = string.Empty;
    [ObservableProperty] private string _emailError = string.Empty;
    [ObservableProperty] private string _passwordError = string.Empty;

    // Summary
    [ObservableProperty] private AppUser? _createdUser;
    [ObservableProperty] private string _summaryText = string.Empty;

    // Collections
    public ObservableCollection<License> AvailableLicenses { get; } = new();
    public ObservableCollection<GroupItem> AvailableGroups { get; } = new();
    public ObservableCollection<AzureRole> AvailableRoles { get; } = new();

    public CreateUserViewModel(
        GraphUserService userService,
        GraphGroupService groupService,
        LicenseService licenseService,
        ILogger<CreateUserViewModel> logger)
    {
        _userService = userService;
        _groupService = groupService;
        _licenseService = licenseService;
        _logger = logger;

        Password = PasswordGenerator.Generate();

        foreach (var role in AzureRole.GetAllowedRoles())
            AvailableRoles.Add(role);

        SelectedRole = AvailableRoles.First();
    }

    partial void OnFirstNameChanged(string value) => UpdateAutoFields();
    partial void OnLastNameChanged(string value) => UpdateAutoFields();

    private void UpdateAutoFields()
    {
        if (!string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName))
        {
            DisplayName = $"{FirstName} {LastName}".Trim();
            var mailNick = GenerateMailNickname(FirstName, LastName);
            if (!string.IsNullOrWhiteSpace(Domain))
                Email = $"{mailNick}@{Domain}";
        }
    }

    private static string GenerateMailNickname(string first, string last)
    {
        var nick = $"{first}.{last}".ToLowerInvariant().Trim('.');
        nick = Regex.Replace(nick, @"[^a-z0-9.]", "");
        return nick;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        HasError = false;

        try
        {
            Domain = await _userService.GetDefaultDomainAsync();

            var licenses = await _licenseService.GetAvailableLicensesAsync();
            AvailableLicenses.Clear();
            AvailableLicenses.Add(new License { SkuId = "", FriendlyName = "No license", SkuPartNumber = "" });
            foreach (var lic in licenses.Where(l => l.HasAvailableUnits))
                AvailableLicenses.Add(lic);
            SelectedLicense = AvailableLicenses.First();

            var groups = await _groupService.GetGroupsForSelectionAsync();
            AvailableGroups.Clear();
            foreach (var g in groups)
                AvailableGroups.Add(g);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load create-user form data.");
            HasError = true;
            ErrorMessage = "Could not load form data. Please check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void GenerateNewPassword()
    {
        Password = PasswordGenerator.Generate();
    }

    public bool Validate()
    {
        bool valid = true;
        FirstNameError = LastNameError = EmailError = PasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            FirstNameError = "First name is required.";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            LastNameError = "Last name is required.";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            EmailError = "A valid email address is required.";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
        {
            PasswordError = "Password must be at least 8 characters.";
            valid = false;
        }

        return valid;
    }

    [RelayCommand]
    public async Task CreateUserAsync()
    {
        if (!Validate()) return;

        IsCreating = true;
        HasError = false;

        try
        {
            var mailNick = GenerateMailNickname(FirstName, LastName);

            CreatedUser = await _userService.CreateUserAsync(
                FirstName,
                LastName,
                DisplayName,
                mailNick,
                Email,
                Password,
                ForceChangePassword,
                string.IsNullOrWhiteSpace(JobTitle) ? null : JobTitle,
                string.IsNullOrWhiteSpace(Department) ? null : Department,
                string.IsNullOrWhiteSpace(OfficeLocation) ? null : OfficeLocation);

            // Assign license.
            if (SelectedLicense is not null && !string.IsNullOrWhiteSpace(SelectedLicense.SkuId))
            {
                await _licenseService.AssignLicenseAsync(CreatedUser.Id, SelectedLicense.SkuId);
            }

            // Add to groups.
            var selectedGroupIds = AvailableGroups.Where(g => g.IsSelected).Select(g => g.Id).ToList();
            if (selectedGroupIds.Any())
            {
                await _groupService.AddUserToGroupsAsync(CreatedUser.Id, selectedGroupIds);
            }

            // Assign role.
            if (SelectedRole is not null && !string.IsNullOrWhiteSpace(SelectedRole.Id))
            {
                await _userService.AssignDirectoryRoleAsync(CreatedUser.Id, SelectedRole.Id);
            }

            // Build summary.
            BuildSummary();
            ShowSummary = true;

            NotificationHelper.ShowSuccess($"{DisplayName} has been created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user.");
            HasError = true;
            ErrorMessage = ex is ApplicationException ? ex.Message : "Could not create the user. Please try again.";
            NotificationHelper.ShowError(ErrorMessage);
        }
        finally
        {
            IsCreating = false;
        }
    }

    private void BuildSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"New Employee Account Created");
        sb.AppendLine($"============================");
        sb.AppendLine($"Name: {DisplayName}");
        sb.AppendLine($"Email: {Email}");
        sb.AppendLine($"Temporary Password: {Password}");
        if (ForceChangePassword)
            sb.AppendLine($"(Must change password on first login)");
        if (!string.IsNullOrWhiteSpace(JobTitle))
            sb.AppendLine($"Job Title: {JobTitle}");
        if (!string.IsNullOrWhiteSpace(Department))
            sb.AppendLine($"Department: {Department}");
        if (!string.IsNullOrWhiteSpace(OfficeLocation))
            sb.AppendLine($"Office: {OfficeLocation}");
        if (SelectedLicense is not null && !string.IsNullOrWhiteSpace(SelectedLicense.SkuId))
            sb.AppendLine($"License: {SelectedLicense.FriendlyName}");
        if (SelectedRole is not null && !string.IsNullOrWhiteSpace(SelectedRole.Id))
            sb.AppendLine($"Admin Role: {SelectedRole.DisplayName}");

        var selectedGroups = AvailableGroups.Where(g => g.IsSelected).ToList();
        if (selectedGroups.Any())
        {
            sb.AppendLine($"Groups: {string.Join(", ", selectedGroups.Select(g => g.DisplayName))}");
        }

        sb.AppendLine();
        sb.AppendLine($"Please sign in at https://portal.office.com with the email and password above.");

        SummaryText = sb.ToString();
    }

    public string GetWelcomeClipboardText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hi {FirstName},");
        sb.AppendLine();
        sb.AppendLine("Your new Microsoft 365 account has been set up. Here are your login details:");
        sb.AppendLine();
        sb.AppendLine($"   Email Address: {Email}");
        sb.AppendLine($"   Temporary Password: {Password}");
        sb.AppendLine();
        sb.AppendLine("To get started:");
        sb.AppendLine("1. Go to https://portal.office.com");
        sb.AppendLine("2. Sign in with the email and password above");
        sb.AppendLine("3. You'll be asked to create a new password on your first login");
        sb.AppendLine();
        sb.AppendLine("If you need any help, please contact the IT department.");
        sb.AppendLine();
        sb.AppendLine("Welcome to the team!");
        return sb.ToString();
    }

    [RelayCommand]
    public void ResetForm()
    {
        FirstName = LastName = DisplayName = Email = string.Empty;
        JobTitle = Department = OfficeLocation = string.Empty;
        Password = PasswordGenerator.Generate();
        ForceChangePassword = true;
        SelectedLicense = AvailableLicenses.FirstOrDefault();
        SelectedRole = AvailableRoles.First();
        ShowSummary = false;
        HasError = false;
        ErrorMessage = string.Empty;
        CreatedUser = null;

        foreach (var g in AvailableGroups)
            g.IsSelected = false;
    }
}
