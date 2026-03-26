namespace M365Manager.Models;

public class AppUser
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string OfficeLocation { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string BusinessPhone { get; set; } = string.Empty;
    public bool AccountEnabled { get; set; } = true;
    public DateTimeOffset? LastSignIn { get; set; }
    public List<LicenseDetail> AssignedLicenses { get; set; } = new();
    public List<GroupItem> GroupMemberships { get; set; } = new();

    public string StatusText => AccountEnabled ? "Active" : "Disabled";
    public string LastSignInText => LastSignIn.HasValue
        ? LastSignIn.Value.LocalDateTime.ToString("g")
        : "Never signed in";
}

public class LicenseDetail
{
    public string SkuId { get; set; } = string.Empty;
    public string SkuPartNumber { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
}
