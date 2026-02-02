using Azure.Identity;
using Microsoft.Graph;
using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public class GraphUserService : IGraphUserService
{
    private readonly GraphServiceClient _graphClient;
    private readonly GraphServiceClient? _delegatedGraphClient;
    private readonly ILogger<GraphUserService> _logger;

    public GraphUserService(
        IConfiguration configuration,
        ILogger<GraphUserService> logger,
        GraphServiceClient? delegatedGraphClient = null)
    {
        _logger = logger;
        _delegatedGraphClient = delegatedGraphClient;

        // Use client credentials flow for app-only access to all users
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "ClientSecret is required for app-only authentication. " +
                "Add it to appsettings.json under AzureAd:ClientSecret");
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // Create auth provider for Graph SDK v4
        var authProvider = new TokenCredentialAuthenticationProvider(
            credential, new[] { "https://graph.microsoft.com/.default" });

        _graphClient = new GraphServiceClient(authProvider);
    }

    public async Task<IReadOnlyList<UserProfile>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<UserProfile>();

        try
        {
            var request = _graphClient.Users
                .Request()
                .Select("id,displayName,jobTitle,department,mail,businessPhones,mobilePhone")
                .Filter("accountEnabled eq true")
                .Top(999);

            var response = await request.GetAsync(cancellationToken);

            if (response?.CurrentPage != null)
            {
                foreach (var user in response.CurrentPage)
                {
                    // Skip users without email (no mailbox to manage)
                    if (!string.IsNullOrEmpty(user.Mail))
                    {
                        users.Add(MapToUserProfile(user));
                    }
                }

                // Handle pagination
                while (response?.NextPageRequest != null)
                {
                    response = await response.NextPageRequest.GetAsync(cancellationToken);
                    if (response?.CurrentPage != null)
                    {
                        foreach (var user in response.CurrentPage)
                        {
                            if (!string.IsNullOrEmpty(user.Mail))
                            {
                                users.Add(MapToUserProfile(user));
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Retrieved {Count} users from Microsoft Graph", users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users from Microsoft Graph");
            throw;
        }

        return users;
    }

    public async Task<UserProfile?> GetUserAsync(string userIdOrEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            // Graph API accepts both userId (GUID) and userPrincipalName (email)
            var user = await _graphClient.Users[userIdOrEmail]
                .Request()
                .Select("id,displayName,jobTitle,department,mail,businessPhones,mobilePhone")
                .GetAsync(cancellationToken);

            return user != null ? MapToUserProfile(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserIdOrEmail} from Microsoft Graph", userIdOrEmail);
            return null;
        }
    }

    public async Task<UserProfile?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _graphClient.Users
                .Request()
                .Filter($"mail eq '{email}' or userPrincipalName eq '{email}'")
                .Select("id,displayName,jobTitle,department,mail,businessPhones,mobilePhone")
                .Top(1)
                .GetAsync(cancellationToken);

            var user = response?.CurrentPage?.FirstOrDefault();
            return user != null ? MapToUserProfile(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email} from Microsoft Graph", email);
            return null;
        }
    }

    public async Task<MailboxSignature> GetUserSignatureAsync(string userId, CancellationToken cancellationToken = default)
    {
        // NOTE: Graph API does not support reading signatures from modern Outlook.
        // This method is kept for compatibility but signatures should be read/written via EWS.
        _logger.LogDebug("GetUserSignatureAsync called for {UserId} - Graph API does not expose new Outlook signatures", userId);

        return new MailboxSignature
        {
            Html = null,
            Text = null,
            IsAccessible = true,
            AccessError = "Use EWS service for signature operations"
        };
    }

    public async Task<bool> UpdateUserAsync(string userId, string? jobTitle, string? department,
        string? businessPhone, string? mobilePhone, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to update user {UserId}: Title={Title}, Dept={Dept}, Phone={Phone}, Mobile={Mobile}",
                userId, jobTitle, department, businessPhone, mobilePhone);

            var userUpdate = new User
            {
                JobTitle = jobTitle,
                Department = department,
                MobilePhone = mobilePhone
            };

            // BusinessPhones is a collection
            if (!string.IsNullOrEmpty(businessPhone))
            {
                userUpdate.BusinessPhones = new List<string> { businessPhone };
            }
            else
            {
                userUpdate.BusinessPhones = new List<string>();
            }

            await _graphClient.Users[userId]
                .Request()
                .UpdateAsync(userUpdate, cancellationToken);

            _logger.LogInformation("Successfully updated user {UserId}", userId);
            return true;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error updating user {UserId}. Status: {Status}, Code: {Code}, Message: {Message}",
                userId, ex.StatusCode, ex.Error?.Code, ex.Error?.Message);
            throw new Exception($"Graph API Error ({ex.StatusCode}): {ex.Error?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfile?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (_delegatedGraphClient == null)
        {
            _logger.LogWarning("GetCurrentUserAsync called but delegated Graph client is not available");
            return null;
        }

        try
        {
            var user = await _delegatedGraphClient.Me
                .Request()
                .Select("id,displayName,jobTitle,department,mail,businessPhones,mobilePhone")
                .GetAsync(cancellationToken);

            if (user != null)
            {
                _logger.LogDebug("Retrieved current user: {DisplayName}", user.DisplayName);
                return MapToUserProfile(user);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user from Microsoft Graph");
            return null;
        }
    }

    private static UserProfile MapToUserProfile(User user)
    {
        return new UserProfile
        {
            Id = user.Id ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            JobTitle = user.JobTitle,
            Department = user.Department,
            Mail = user.Mail,
            BusinessPhone = user.BusinessPhones?.FirstOrDefault(),
            MobilePhone = user.MobilePhone
        };
    }
}
