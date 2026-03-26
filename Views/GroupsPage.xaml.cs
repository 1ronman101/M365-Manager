using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using M365Manager.Models;
using M365Manager.ViewModels;

namespace M365Manager.Views;

public sealed partial class GroupsPage : Page
{
    public GroupsViewModel ViewModel { get; }

    public GroupsPage()
    {
        ViewModel = App.Services.GetRequiredService<GroupsViewModel>();
        this.InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadGroupsAsync();
    }

    private async void ViewGroupMembers_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Models.GroupItem group)
        {
            await ViewModel.SelectGroupAsync(group);
        }
    }

    private void BackToGroups_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearGroupSelectionCommand.Execute(null);
    }

    private async void SearchMemberToAdd_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SearchUsersToAddAsync();
    }

    private async void AddMemberSearch_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.SearchUsersToAddAsync();
        }
    }

    private async void AddMember_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AppUser user)
        {
            await ViewModel.AddMemberAsync(user);
        }
    }

    private async void RemoveMember_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GroupMember member)
        {
            await ViewModel.RemoveMemberAsync(member);
        }
    }
}
