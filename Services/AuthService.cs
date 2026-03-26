using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Azure.Identity;
using M365Manager.Helpers;
using System.Net.Http.Headers;

namespace M365Manager.Services;

/// <summary>
/// Handles all Microsoft identity authentication using MSAL with Authorization Code + PKCE.
/// Manages token acquisition, silent refresh, and exposes a GraphServiceClient.
/// </summary>
public class AuthService
{
    private readonly ILogger<AuthService> _logger;
    private IPublicClientApplication? _msalClient;
    private IAccount? _currentAccount;
    private GraphServiceClient? _graphClient;

    public static readonly string[] Scopes = new[]
    {
        "User.ReadWrite.All",
        "Directory.ReadWrite.All",
        "Group.ReadWrite.All",
        "UserAuthenticationMethod.ReadWrite.All"
    };

    public bool IsAuthenticated => _currentAccount is not null;
    public string? UserDisplayName => _currentAccount?.Username;

    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialises the MSAL public client. Call once when settings are available.
    /// </summary>
    public void Initialize(AppSettings settings)
    {
        _msalClient = PublicClientApplicationBuilder
            .Create(settings.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, settings.TenantId)
            .WithRedirectUri(settings.RedirectUri)
            .WithLogging((level, message, containsPii) =>
            {
                if (!containsPii)
                    _logger.LogDebug("[MSAL] {Message}", message);
            }, Microsoft.Identity.Client.LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: false)
            .Build();

        // Use MSAL's built-in in-memory token cache (tokens are never written to disk).
        TokenCacheHelper.EnableSerialization(_msalClient.UserTokenCache);
    }

    /// <summary>
    /// Attempts silent sign-in first, falls back to interactive browser sign-in.
    /// </summary>
    public async Task<bool> SignInAsync()
    {
        if (_msalClient is null) throw new InvalidOperationException("AuthService not initialised. Call Initialize first.");

        try
        {
            // Try silent first.
            var accounts = await _msalClient.GetAccountsAsync();
            _currentAccount = accounts.FirstOrDefault();

            if (_currentAccount is not null)
            {
                var silentResult = await _msalClient
                    .AcquireTokenSilent(Scopes, _currentAccount)
                    .ExecuteAsync();

                BuildGraphClient(silentResult.AccessToken);
                _logger.LogInformation("Silent sign-in succeeded for {User}.", _currentAccount.Username);
                return true;
            }
        }
        catch (MsalUiRequiredException)
        {
            _logger.LogInformation("Silent sign-in failed; falling back to interactive.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Silent auth attempt failed.");
        }

        // Interactive sign-in.
        try
        {
            var interactiveResult = await _msalClient
                .AcquireTokenInteractive(Scopes)
                .WithPrompt(Prompt.SelectAccount)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync();

            _currentAccount = interactiveResult.Account;
            BuildGraphClient(interactiveResult.AccessToken);
            _logger.LogInformation("Interactive sign-in succeeded for {User}.", _currentAccount.Username);
            return true;
        }
        catch (MsalServiceException ex) when (ex.ErrorCode == "access_denied")
        {
            _logger.LogError(ex, "Access denied. The account may lack admin permissions.");
            throw new UnauthorizedAccessException(
                "Your account doesn't have the required administrator permissions. " +
                "Please sign in with an account that has admin access, or ask your IT administrator to grant the necessary permissions.");
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
        {
            _logger.LogInformation("User cancelled sign-in.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign-in failed.");
            throw;
        }
    }

    /// <summary>
    /// Obtains a valid access token, refreshing silently if needed.
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        if (_msalClient is null || _currentAccount is null)
            throw new InvalidOperationException("Not authenticated.");

        try
        {
            var result = await _msalClient
                .AcquireTokenSilent(Scopes, _currentAccount)
                .ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // Token expired and refresh failed; need interactive.
            var result = await _msalClient
                .AcquireTokenInteractive(Scopes)
                .WithAccount(_currentAccount)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();
            _currentAccount = result.Account;
            BuildGraphClient(result.AccessToken);
            return result.AccessToken;
        }
    }

    /// <summary>
    /// Returns the cached GraphServiceClient, rebuilding if needed.
    /// </summary>
    public async Task<GraphServiceClient> GetGraphClientAsync()
    {
        if (_graphClient is not null)
        {
            // Ensure token is still valid by acquiring silently.
            var token = await GetAccessTokenAsync();
            BuildGraphClient(token);
        }
        else
        {
            var token = await GetAccessTokenAsync();
            BuildGraphClient(token);
        }

        return _graphClient!;
    }

    /// <summary>
    /// Signs out the current account.
    /// </summary>
    public async Task SignOutAsync()
    {
        if (_msalClient is not null && _currentAccount is not null)
        {
            await _msalClient.RemoveAsync(_currentAccount);
            _currentAccount = null;
            _graphClient = null;
            _logger.LogInformation("User signed out.");
        }
    }

    /// <summary>
    /// Tests the connection by making a simple Graph call.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            var client = await GetGraphClientAsync();
            var me = await client.Me.GetAsync();
            return (true, $"Connected as {me?.DisplayName} ({me?.UserPrincipalName})");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Your account lacks the required admin permissions.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    private void BuildGraphClient(string accessToken)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new TokenProvider(accessToken));
        _graphClient = new GraphServiceClient(new HttpClient(), authProvider);
    }

    /// <summary>
    /// Simple token provider wrapping a raw access token.
    /// </summary>
    private class TokenProvider : IAccessTokenProvider
    {
        private readonly string _token;
        public TokenProvider(string token) => _token = token;

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_token);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }

    /// <summary>
    /// Minimal in-memory token cache serialization helper.
    /// Tokens live in memory only; nothing is persisted to disk.
    /// </summary>
    private static class TokenCacheHelper
    {
        private static readonly object FileLock = new();
        private static byte[] _cacheData = Array.Empty<byte>();

        public static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(_cacheData.Length > 0 ? _cacheData : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    _cacheData = args.TokenCache.SerializeMsalV3();
                }
            }
        }
    }
}

/// <summary>
/// Implementation of Microsoft.Kiota.Abstractions.Authentication.IAuthenticationProvider
/// that injects a bearer token into outgoing requests.
/// </summary>
public class BaseBearerTokenAuthenticationProvider : Microsoft.Kiota.Abstractions.Authentication.IAuthenticationProvider
{
    private readonly IAccessTokenProvider _tokenProvider;

    public BaseBearerTokenAuthenticationProvider(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public async Task AuthenticateRequestAsync(
        Microsoft.Kiota.Abstractions.RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _tokenProvider.GetAuthorizationTokenAsync(
            request.URI,
            additionalAuthenticationContext,
            cancellationToken);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }
    }
}

public interface IAccessTokenProvider
{
    Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default);
    AllowedHostsValidator AllowedHostsValidator { get; }
}

public class AllowedHostsValidator
{
    public IEnumerable<string> AllowedHosts { get; set; } = new[] { "graph.microsoft.com" };
}
