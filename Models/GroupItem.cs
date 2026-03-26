namespace M365Manager.Models;

public class GroupItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsTeam { get; set; }
    public List<string> GroupTypes { get; set; } = new();

    public string TypeLabel => IsTeam ? "Team" : "Group";
    public string MemberCountText => MemberCount == 1 ? "1 member" : $"{MemberCount} members";
    public string Icon => IsTeam ? "\uE902" : "\uE716";

    public bool IsSelected { get; set; }
}

public class GroupMember
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
}

public class AzureRole
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Returns safe roles that non-IT users may assign.
    /// Global Admin is intentionally excluded for safety.
    /// </summary>
    public static List<AzureRole> GetAllowedRoles() => new()
    {
        new() { Id = "", DisplayName = "Standard User (no admin role)", Description = "Regular user with no administrative privileges." },
        new() { Id = "f2ef992c-3afb-46b9-b7cf-a126ee74c451", DisplayName = "Global Reader", Description = "Can read everything a Global Admin can, but cannot make changes." },
        new() { Id = "729827e3-9c14-49f7-bb1b-9608f156bbb8", DisplayName = "Helpdesk Administrator", Description = "Can reset passwords for non-admin users and Helpdesk Admins." },
        new() { Id = "fe930be7-5e62-47db-91af-98c3a49a38b1", DisplayName = "User Administrator", Description = "Can manage users and groups, including resetting passwords." },
    };
}
