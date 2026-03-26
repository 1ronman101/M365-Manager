using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using M365Manager.Models;

namespace M365Manager.Services;

/// <summary>
/// Wraps all Microsoft Graph API calls related to group and team management.
/// </summary>
public class GraphGroupService
{
    private readonly AuthService _authService;
    private readonly ILogger<GraphGroupService> _logger;

    public GraphGroupService(AuthService authService, ILogger<GraphGroupService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all Microsoft 365 Groups (Unified groups) in the tenant.
    /// </summary>
    public async Task<List<GroupItem>> GetAllGroupsAsync()
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var groups = await client.Groups.GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "displayName", "description", "mail", "groupTypes", "resourceProvisioningOptions"
                };
                config.QueryParameters.Filter = "groupTypes/any(g:g eq 'Unified')";
                config.QueryParameters.Top = 999;
                config.QueryParameters.Orderby = new[] { "displayName" };
            });

            var items = new List<GroupItem>();

            if (groups?.Value is not null)
            {
                foreach (var g in groups.Value)
                {
                    var item = new GroupItem
                    {
                        Id = g.Id ?? "",
                        DisplayName = g.DisplayName ?? "",
                        Description = g.Description ?? "",
                        Mail = g.Mail ?? "",
                        GroupTypes = g.GroupTypes?.ToList() ?? new(),
                        IsTeam = g.AdditionalData?.ContainsKey("resourceProvisioningOptions") == true && ((System.Text.Json.JsonElement?)g.AdditionalData["resourceProvisioningOptions"])?.EnumerateArray().Any(x => x.GetString() == "Team") == true
                    };

                    // Get member count.
                    try
                    {
                        var members = await client.Groups[g.Id].Members.GetAsync(config =>
                        {
                            config.QueryParameters.Select = new[] { "id" };
                            config.QueryParameters.Top = 999;
                            config.Headers.Add("ConsistencyLevel", "eventual");
                            config.QueryParameters.Count = true;
                        });
                        item.MemberCount = members?.Value?.Count ?? 0;
                    }
                    catch
                    {
                        item.MemberCount = 0;
                    }

                    items.Add(item);
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get groups.");
            throw new ApplicationException("Could not load teams and groups. Please check your connection.", ex);
        }
    }

    /// <summary>
    /// Gets the members of a specific group.
    /// </summary>
    public async Task<List<GroupMember>> GetGroupMembersAsync(string groupId)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var members = await client.Groups[groupId].Members.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle" };
                config.QueryParameters.Top = 999;
            });

            return members?.Value?
                .OfType<User>()
                .Select(u => new GroupMember
                {
                    Id = u.Id ?? "",
                    DisplayName = u.DisplayName ?? "",
                    Email = u.Mail ?? u.UserPrincipalName ?? "",
                    JobTitle = u.JobTitle ?? ""
                })
                .OrderBy(m => m.DisplayName)
                .ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get members for group: {GroupId}", groupId);
            throw new ApplicationException("Could not load group members. Please try again.", ex);
        }
    }

    /// <summary>
    /// Adds a user to a group.
    /// </summary>
    public async Task AddMemberAsync(string groupId, string userId)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            await client.Groups[groupId].Members.Ref.PostAsync(
                new ReferenceCreate
                {
                    OdataId = $"https://graph.microsoft.com/v1.0/users/{userId}"
                });

            _logger.LogInformation("Added user {UserId} to group {GroupId}.", userId, groupId);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
            when (odataEx.Error?.Code == "Request_BadRequest" &&
                  (odataEx.Error.Message?.Contains("already exist") == true))
        {
            _logger.LogInformation("User {UserId} is already a member of group {GroupId}.", userId, groupId);
            throw new ApplicationException("This person is already a member of this group.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user {UserId} to group {GroupId}.", userId, groupId);
            throw new ApplicationException("Could not add the member to the group. Please try again.", ex);
        }
    }

    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    public async Task RemoveMemberAsync(string groupId, string userId)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();
            await client.Groups[groupId].Members[userId].Ref.DeleteAsync();
            _logger.LogInformation("Removed user {UserId} from group {GroupId}.", userId, groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove user {UserId} from group {GroupId}.", userId, groupId);
            throw new ApplicationException("Could not remove the member from the group. Please try again.", ex);
        }
    }

    /// <summary>
    /// Gets groups for use in a multi-select checklist (e.g. during user creation).
    /// </summary>
    public async Task<List<GroupItem>> GetGroupsForSelectionAsync()
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var groups = await client.Groups.GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "displayName", "description", "groupTypes", "resourceProvisioningOptions"
                };
                config.QueryParameters.Filter = "groupTypes/any(g:g eq 'Unified')";
                config.QueryParameters.Top = 999;
                config.QueryParameters.Orderby = new[] { "displayName" };
            });

            return groups?.Value?.Select(g => new GroupItem
            {
                Id = g.Id ?? "",
                DisplayName = g.DisplayName ?? "",
                Description = g.Description ?? "",
                GroupTypes = g.GroupTypes?.ToList() ?? new(),
                IsTeam = g.AdditionalData?.ContainsKey("resourceProvisioningOptions") == true && ((System.Text.Json.JsonElement?)g.AdditionalData["resourceProvisioningOptions"])?.EnumerateArray().Any(x => x.GetString() == "Team") == true,
                IsSelected = false
            }).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get groups for selection.");
            throw new ApplicationException("Could not load the list of teams and groups.", ex);
        }
    }

    /// <summary>
    /// Adds a user to multiple groups at once.
    /// </summary>
    public async Task AddUserToGroupsAsync(string userId, IEnumerable<string> groupIds)
    {
        var client = await _authService.GetGraphClientAsync();

        foreach (var groupId in groupIds)
        {
            try
            {
                await client.Groups[groupId].Members.Ref.PostAsync(
                    new ReferenceCreate
                    {
                        OdataId = $"https://graph.microsoft.com/v1.0/users/{userId}"
                    });
                _logger.LogInformation("Added user {UserId} to group {GroupId}.", userId, groupId);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
                when (odataEx.Error?.Message?.Contains("already exist") == true)
            {
                _logger.LogInformation("User {UserId} already in group {GroupId}, skipping.", userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add user {UserId} to group {GroupId}.", userId, groupId);
            }
        }
    }
}
