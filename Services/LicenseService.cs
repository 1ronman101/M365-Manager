using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using M365Manager.Models;

namespace M365Manager.Services;

/// <summary>
/// Wraps all Microsoft Graph API calls related to license management.
/// </summary>
public class LicenseService
{
    private readonly AuthService _authService;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(AuthService authService, ILogger<LicenseService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all subscribed SKUs (licenses) in the tenant.
    /// </summary>
    public async Task<List<License>> GetAvailableLicensesAsync()
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();
            var skus = await client.SubscribedSkus.GetAsync();

            return skus?.Value?
                .Where(s => s.CapabilityStatus == "Enabled")
                .Select(s => new License
                {
                    SkuId = s.SkuId?.ToString() ?? "",
                    SkuPartNumber = s.SkuPartNumber ?? "",
                    FriendlyName = License.GetFriendlyName(s.SkuPartNumber ?? ""),
                    TotalUnits = (int)(s.PrepaidUnits?.Enabled ?? 0),
                    ConsumedUnits = (int)(s.ConsumedUnits ?? 0)
                })
                .OrderBy(l => l.FriendlyName)
                .ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available licenses.");
            throw new ApplicationException("Could not load the list of available licenses. Please check your permissions.", ex);
        }
    }

    /// <summary>
    /// Assigns a license to a user.
    /// </summary>
    public async Task AssignLicenseAsync(string userId, string skuId)
    {
        if (string.IsNullOrWhiteSpace(skuId)) return;

        try
        {
            var client = await _authService.GetGraphClientAsync();

            var body = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
            {
                AddLicenses = new List<AssignedLicense>
                {
                    new() { SkuId = Guid.Parse(skuId) }
                },
                RemoveLicenses = new List<Guid?>()
            };

            await client.Users[userId].AssignLicense.PostAsync(body);
            _logger.LogInformation("Assigned license {SkuId} to user {UserId}.", skuId, userId);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
        {
            _logger.LogError(odataEx, "Graph error assigning license to user: {UserId}", userId);
            var msg = odataEx.Error?.Message ?? odataEx.Message;
            if (msg.Contains("UsageLocation", StringComparison.OrdinalIgnoreCase))
                throw new ApplicationException("Cannot assign a license because the user's usage location is not set. Please update their profile first.");
            throw new ApplicationException($"Could not assign the license: {msg}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign license to user: {UserId}", userId);
            throw new ApplicationException("Could not assign the license. Please try again.", ex);
        }
    }

    /// <summary>
    /// Removes a license from a user.
    /// </summary>
    public async Task RemoveLicenseAsync(string userId, string skuId)
    {
        if (string.IsNullOrWhiteSpace(skuId)) return;

        try
        {
            var client = await _authService.GetGraphClientAsync();

            var body = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
            {
                AddLicenses = new List<AssignedLicense>(),
                RemoveLicenses = new List<Guid?> { Guid.Parse(skuId) }
            };

            await client.Users[userId].AssignLicense.PostAsync(body);
            _logger.LogInformation("Removed license {SkuId} from user {UserId}.", skuId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove license from user: {UserId}", userId);
            throw new ApplicationException("Could not remove the license. Please try again.", ex);
        }
    }

    /// <summary>
    /// Changes a user's license (removes old, assigns new).
    /// </summary>
    public async Task ChangeLicenseAsync(string userId, string oldSkuId, string newSkuId)
    {
        try
        {
            var client = await _authService.GetGraphClientAsync();

            var removeLicenses = new List<Guid?>();
            if (!string.IsNullOrWhiteSpace(oldSkuId))
                removeLicenses.Add(Guid.Parse(oldSkuId));

            var addLicenses = new List<AssignedLicense>();
            if (!string.IsNullOrWhiteSpace(newSkuId))
                addLicenses.Add(new AssignedLicense { SkuId = Guid.Parse(newSkuId) });

            var body = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
            {
                AddLicenses = addLicenses,
                RemoveLicenses = removeLicenses
            };

            await client.Users[userId].AssignLicense.PostAsync(body);
            _logger.LogInformation("Changed license for user {UserId}: removed {Old}, added {New}.", userId, oldSkuId, newSkuId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change license for user: {UserId}", userId);
            throw new ApplicationException("Could not change the license assignment. Please try again.", ex);
        }
    }
}
