using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using M365Manager.Helpers;
using M365Manager.Models;
using M365Manager.Services;
using System.Collections.ObjectModel;

namespace M365Manager.ViewModels;

public partial class GroupsViewModel : ObservableObject
{
    private readonly GraphGroupService _groupService;
    private readonly GraphUserService _userService;
    private readonly ILogger<GroupsViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isLoadingMembers;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private GroupItem? _selectedGroup;
    [ObservableProperty] private bool _isGroupSelected;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _addMemberSearch = string.Empty;
    [ObservableProperty] private bool _isSearchingMembers;
    [ObservableProperty] private AppUser? _selectedUserToAdd;

    public ObservableCollection<GroupItem> Groups { get; } = new();
    public ObservableCollection<GroupMember> Members { get; } = new();
    public ObservableCollection<AppUser> MemberSearchResults { get; } = new();

    public GroupsViewModel(
        GraphGroupService groupService,
        GraphUserService userService,
        ILogger<GroupsViewModel> logger)
    {
        _groupService = groupService;
        _userService = userService;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadGroupsAsync()
    {
        IsLoading = true;
        Groups.Clear();

        try
        {
            var groups = await _groupService.GetAllGroupsAsync();
            foreach (var g in groups)
                Groups.Add(g);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups.");
            NotificationHelper.ShowError("Could not load teams and groups. Please check your connection.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectGroupAsync(GroupItem group)
    {
        SelectedGroup = group;
        IsGroupSelected = true;
        IsLoadingMembers = true;
        Members.Clear();

        try
        {
            var members = await _groupService.GetGroupMembersAsync(group.Id);
            foreach (var m in members)
                Members.Add(m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load members for group: {GroupId}", group.Id);
            NotificationHelper.ShowError("Could not load group members.");
        }
        finally
        {
            IsLoadingMembers = false;
        }
    }

    [RelayCommand]
    public async Task SearchUsersToAddAsync()
    {
        if (string.IsNullOrWhiteSpace(AddMemberSearch) || AddMemberSearch.Length < 2) return;

        IsSearchingMembers = true;
        MemberSearchResults.Clear();

        try
        {
            var results = await _userService.SearchUsersAsync(AddMemberSearch);
            var existingIds = Members.Select(m => m.Id).ToHashSet();
            foreach (var user in results.Where(u => !existingIds.Contains(u.Id)))
                MemberSearchResults.Add(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Member search failed.");
            NotificationHelper.ShowError("Could not search for users.");
        }
        finally
        {
            IsSearchingMembers = false;
        }
    }

    [RelayCommand]
    public async Task AddMemberAsync(AppUser user)
    {
        if (SelectedGroup is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Add Member",
            $"Add {user.DisplayName} to {SelectedGroup.DisplayName}?",
            "Yes, add them");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _groupService.AddMemberAsync(SelectedGroup.Id, user.Id);
            await SelectGroupAsync(SelectedGroup);
            MemberSearchResults.Clear();
            AddMemberSearch = string.Empty;
            NotificationHelper.ShowSuccess($"{user.DisplayName} has been added to {SelectedGroup.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not add the member.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RemoveMemberAsync(GroupMember member)
    {
        if (SelectedGroup is null) return;

        var confirmed = await DialogHelper.ShowConfirmationAsync(
            "Remove Member",
            $"Remove {member.DisplayName} from {SelectedGroup.DisplayName}?",
            "Yes, remove them");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _groupService.RemoveMemberAsync(SelectedGroup.Id, member.Id);
            await SelectGroupAsync(SelectedGroup);
            NotificationHelper.ShowSuccess($"{member.DisplayName} has been removed from {SelectedGroup.DisplayName}.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(ex is ApplicationException ? ex.Message : "Could not remove the member.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ClearGroupSelection()
    {
        SelectedGroup = null;
        IsGroupSelected = false;
        Members.Clear();
    }
}
