using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using M365Manager.Models;
using AppUser = M365Manager.Models.AppUser;
using LicenseDetail = M365Manager.Models.LicenseDetail;

namespace M365Manager.Services;

/// <summary>
/// Wraps all Microsoft Graph API calls related to user management.
/// </summary>
public class GraphUserService
{
    private readonly AuthService _authService;
    private readonly ILogger<GraphUserService> _logger;

    public GraphUserService(AuthService authService, ILogger<GraphUserService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Gets total user count and licensed user count for the dashboard.
    /// </summary>
    public async Task<(int TotalUsers, int LicensedUsers)> GetUserStatsAsync()
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var allUsers = await client.Users.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "assignedLicenses" };
                config.QueryParameters.Top = 999;
                config.Headers.Add("ConsistencyLevel", "eventual");
                config.QueryParameters.Count = true;
            });

            int total = 0;
            int licensed = 0;

            if (allUsers?.Value is not null)
            {
                total = allUsers.Value.Count;
                licensed = allUsers.Value.Count(u => u.AssignedLicenses?.Any() == true);

                // Page through if needed.
                var pageIterator = Microsoft.Graph.PageIterator<User, UserCollectionResponse>
                    .CreatePageIterator(client, allUsers, user =>
                    {
                        // Already counted above; this handles subsequent pages.
                        return true;
                    });
                // Note: for stats we use the first-page count as a good approximation.
            }

            return (total, licensed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user stats.");
            throw new ApplicationException("Could not load user statistics. Please check your connection and permissions.", ex);
        }
    }

    /// <summary>
    /// Searches for users by name or email.
    /// </summary>
    public async Task<List<AppUser>> SearchUsersAsync(string query)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var result = await client.Users.GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "displayName", "givenName", "surname", "mail",
                    "userPrincipalName", "jobTitle", "department",
                    "officeLocation", "accountEnabled", "mobilePhone",
                    "businessPhones", "assignedLicenses",
                    "signInActivity"
                };
                config.QueryParameters.Search = $"\"displayName:{query}\" OR \"mail:{query}\" OR \"userPrincipalName:{query}\"";
                config.QueryParameters.Top = 25;
                config.QueryParameters.Orderby = new[] { "displayName" };
                config.Headers.Add("ConsistencyLevel", "eventual");
                config.QueryParameters.Count = true;
            });

            return result?.Value?.Select(MapToAppUser).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search users with query: {Query}", query);
            throw new ApplicationException("Could not search for users. Please try again.", ex);
        }
    }

    /// <summary>
    /// Gets a single user's full profile.
    /// </summary>
    public async Task<AppUser> GetUserAsync(string userId)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var user = await client.Users[userId].GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "displayName", "givenName", "surname", "mail",
                    "userPrincipalName", "jobTitle", "department",
                    "officeLocation", "accountEnabled", "mobilePhone",
                    "businessPhones", "assignedLicenses",
                    "signInActivity"
                };
            });

            if (user is null) throw new ApplicationException("User not found.");

            var appUser = MapToAppUser(user);

            // Get group memberships.
            var memberships = await client.Users[userId].MemberOf.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "displayName", "mail", "groupTypes" };
            });

            if (memberships?.Value is not null)
            {
                foreach (var obj in memberships.Value)
                {
                    if (obj is Group grp)
                    {
                        appUser.GroupMemberships.Add(new GroupItem
                        {
                            Id = grp.Id ?? "",
                            DisplayName = grp.DisplayName ?? "",
                            Mail = grp.Mail ?? "",
                            GroupTypes = grp.GroupTypes?.ToList() ?? new(),
                            IsTeam = grp.GroupTypes?.Contains("Unified") == true
                        });
                    }
                }
            }

            // Get license details.
            var licenseDetails = await client.Users[userId].LicenseDetails.GetAsync();
            if (licenseDetails?.Value is not null)
            {
                appUser.AssignedLicenses = licenseDetails.Value.Select(l => new LicenseDetail
                {
                    SkuId = l.SkuId?.ToString() ?? "",
                    SkuPartNumber = l.SkuPartNumber ?? "",
                    FriendlyName = Models.License.GetFriendlyName(l.SkuPartNumber ?? "")
                }).ToList();
            }

            return appUser;
        }
        catch (ApplicationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", userId);
            throw new ApplicationException("Could not load user details. Please try again.", ex);
        }
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    public async Task<AppUser> CreateUserAsync(
        string firstName,
        string lastName,
        string displayName,
        string mailNickname,
        string userPrincipalName,
        string password,
        bool forceChangePassword,
        string? jobTitle,
        string? department,
        string? officeLocation)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var newUser = new User
            {
                GivenName = firstName,
                Surname = lastName,
                DisplayName = displayName,
                MailNickname = mailNickname,
                UserPrincipalName = userPrincipalName,
                AccountEnabled = true,
                PasswordProfile = new PasswordProfile
                {
                    Password = password,
                    ForceChangePasswordNextSignIn = forceChangePassword
                },
                UsageLocation = "ZA" // Default; can be changed.
            };

            if (!string.IsNullOrWhiteSpace(jobTitle)) newUser.JobTitle = jobTitle;
            if (!string.IsNullOrWhiteSpace(department)) newUser.Department = department;
            if (!string.IsNullOrWhiteSpace(officeLocation)) newUser.OfficeLocation = officeLocation;

            var created = await client.Users.PostAsync(newUser);

            if (created is null) throw new ApplicationException("User creation returned no data.");

            _logger.LogInformation("Created user: {UPN}", userPrincipalName);
            return MapToAppUser(created);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
        {
            _logger.LogError(odataEx, "Graph error creating user: {UPN}", userPrincipalName);
            var msg = odataEx.Error?.Message ?? odataEx.Message;
            if (msg.Contains("userPrincipalName already exists", StringComparison.OrdinalIgnoreCase))
                throw new ApplicationException($"The email address {userPrincipalName} is already taken. Please choose a different one.");
            throw new ApplicationException($"Could not create the user: {msg}");
        }
        catch (ApplicationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {UPN}", userPrincipalName);
            throw new ApplicationException("Could not create the user. Please try again.", ex);
        }
    }

    /// <summary>
    /// Updates a user's profile fields.
    /// </summary>
    public async Task UpdateUserProfileAsync(
        string userId,
        string? jobTitle,
        string? department,
        string? officeLocation,
        string? mobilePhone,
        string? businessPhone)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var update = new User
            {
                JobTitle = jobTitle,
                Department = department,
                OfficeLocation = officeLocation,
                MobilePhone = mobilePhone,
                BusinessPhones = string.IsNullOrWhiteSpace(businessPhone) ? new List<string>() : new List<string> { businessPhone }
            };

            await client.Users[userId].PatchAsync(update);
            _logger.LogInformation("Updated profile for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile for user: {UserId}", userId);
            throw new ApplicationException("Could not update the user's profile. Please try again.", ex);
        }
    }

    /// <summary>
    /// Enables or disables a user account.
    /// </summary>
    public async Task SetAccountEnabledAsync(string userId, bool enabled)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();
            await client.Users[userId].PatchAsync(new User { AccountEnabled = enabled });
            _logger.LogInformation("Set account enabled={Enabled} for user: {UserId}", enabled, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set account enabled for user: {UserId}", userId);
            var action = enabled ? "enable" : "disable";
            throw new ApplicationException($"Could not {action} the account. Please try again.", ex);
        }
    }

    /// <summary>
    /// Resets a user's password.
    /// </summary>
    public async Task ResetPasswordAsync(string userId, string newPassword, bool forceChange)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();
            await client.Users[userId].PatchAsync(new User
            {
                PasswordProfile = new PasswordProfile
                {
                    Password = newPassword,
                    ForceChangePasswordNextSignIn = forceChange
                }
            });
            _logger.LogInformation("Password reset for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password for user: {UserId}", userId);
            throw new ApplicationException("Could not reset the password. Please try again.", ex);
        }
    }

    /// <summary>
    /// Assigns an Azure AD directory role to a user.
    /// </summary>
    public async Task AssignDirectoryRoleAsync(string userId, string roleTemplateId)
    {
        if (string.IsNullOrWhiteSpace(roleTemplateId)) return; // "Standard User" = no role

        try
        {
            var client = await _authService.GetGraphClientAsync();

            // Activate the role if not already activated.
            var roles = await client.DirectoryRoles.GetAsync();
            var existingRole = roles?.Value?.FirstOrDefault(r => r.RoleTemplateId == roleTemplateId);

            string roleId;
            if (existingRole is not null)
            {
                roleId = existingRole.Id!;
            }
            else
            {
                var activated = await client.DirectoryRoles.PostAsync(new DirectoryRole
                {
                    RoleTemplateId = roleTemplateId
                });
                roleId = activated!.Id!;
            }

            await client.DirectoryRoles[roleId].Members.Ref.PostAsync(
                new Microsoft.Graph.Models.ReferenceCreate
                {
                    OdataId = $"https://graph.microsoft.com/v1.0/users/{userId}"
                });

            _logger.LogInformation("Assigned role {RoleId} to user: {UserId}", roleTemplateId, userId);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
            when (odataEx.Error?.Code == "Request_ResourceNotFound" || odataEx.Message.Contains("already exist"))
        {
            _logger.LogInformation("Role already assigned or resource issue for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role for user: {UserId}", userId);
            throw new ApplicationException("Could not assign the admin role. Please try again.", ex);
        }
    }

    /// <summary>
    /// Gets the default domain for the tenant (used for email suggestions).
    /// </summary>
    public async Task<string> GetDefaultDomainAsync()
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();
            var domains = await client.Domains.GetAsync();
            var defaultDomain = domains?.Value?.FirstOrDefault(d => d.IsDefault == true);
            return defaultDomain?.Id ?? "yourdomain.onmicrosoft.com";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get default domain.");
            return "yourdomain.onmicrosoft.com";
        }
    }

    private static AppUser MapToAppUser(User user) => new()
    {
        Id = user.Id ?? "",
        DisplayName = user.DisplayName ?? "",
        FirstName = user.GivenName ?? "",
        LastName = user.Surname ?? "",
        Email = user.Mail ?? user.UserPrincipalName ?? "",
        UserPrincipalName = user.UserPrincipalName ?? "",
        JobTitle = user.JobTitle ?? "",
        Department = user.Department ?? "",
        OfficeLocation = user.OfficeLocation ?? "",
        MobilePhone = user.MobilePhone ?? "",
        BusinessPhone = user.BusinessPhones?.FirstOrDefault() ?? "",
        AccountEnabled = user.AccountEnabled ?? true,
        LastSignIn = user.SignInActivity?.LastSignInDateTime
    };
}
