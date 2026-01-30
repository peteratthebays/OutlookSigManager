using Azure.Identity;
using Microsoft.Exchange.WebServices.Data;
using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public class EwsSignatureService : IExchangeSignatureService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EwsSignatureService> _logger;
    private readonly ClientSecretCredential _credential;

    public EwsSignatureService(IConfiguration configuration, ILogger<EwsSignatureService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("ClientSecret is required for EWS authentication.");
        }

        _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    private async Task<ExchangeService> CreateExchangeServiceAsync(string userEmail, CancellationToken cancellationToken)
    {
        // Get OAuth token for EWS
        var tokenContext = new Azure.Core.TokenRequestContext(new[] { "https://outlook.office365.com/.default" });
        var token = await _credential.GetTokenAsync(tokenContext, cancellationToken);

        var service = new ExchangeService(ExchangeVersion.Exchange2016)
        {
            Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx"),
            Credentials = new OAuthCredentials(token.Token),
            ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, userEmail)
        };

        return service;
    }

    public async Task<MailboxSignature> GetSignatureAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        // NOTE: Reading signatures from modern Exchange Online / new Outlook is NOT supported via EWS or Graph API.
        // Microsoft stores new Outlook signatures in a cloud-only location (Substrate) that's only accessible via PowerShell.
        // This method returns a result indicating signatures cannot be read, but the tool can still DEPLOY signatures.

        _logger.LogInformation("Signature read requested for {UserEmail} - Note: New Outlook signatures are not readable via API", userEmail);

        try
        {
            // Verify we can connect to the mailbox (validates permissions)
            var service = await CreateExchangeServiceAsync(userEmail, cancellationToken);

            // Try to read legacy OWA.UserOptions in case user has classic signatures
            try
            {
                var userConfig = await UserConfiguration.Bind(
                    service,
                    "OWA.UserOptions",
                    WellKnownFolderName.Root,
                    UserConfigurationProperties.Dictionary);

                string? signatureHtml = null;
                string? signatureText = null;

                if (userConfig.Dictionary.ContainsKey("signaturehtml"))
                {
                    signatureHtml = userConfig.Dictionary["signaturehtml"] as string;
                }
                if (userConfig.Dictionary.ContainsKey("signaturetext"))
                {
                    signatureText = userConfig.Dictionary["signaturetext"] as string;
                }

                if (!string.IsNullOrEmpty(signatureHtml) || !string.IsNullOrEmpty(signatureText))
                {
                    _logger.LogInformation("Found legacy signature for {UserEmail}", userEmail);
                    return new MailboxSignature
                    {
                        Html = signatureHtml,
                        Text = signatureText,
                        IsAccessible = true,
                        AccessError = null
                    };
                }
            }
            catch (ServiceResponseException ex) when (ex.ErrorCode == ServiceError.ErrorItemNotFound)
            {
                // No legacy config - this is normal for new Outlook users
            }

            // Return accessible but no signature found
            // This means: we have access, but signatures are in cloud-only storage (new Outlook)
            return new MailboxSignature
            {
                Html = null,
                Text = null,
                IsAccessible = true,
                AccessError = "Signatures in new Outlook are stored in cloud-only format and cannot be read via API. Use 'Deploy Signature' to set a standardized signature."
            };
        }
        catch (ServiceResponseException ex) when (ex.ErrorCode == ServiceError.ErrorAccessDenied)
        {
            _logger.LogWarning("Access denied for {UserEmail}: {Error}", userEmail, ex.Message);
            return new MailboxSignature
            {
                Html = null,
                Text = null,
                IsAccessible = false,
                AccessError = "Access denied. Ensure the app has ApplicationImpersonation role in Exchange."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing mailbox for {UserEmail}", userEmail);
            return new MailboxSignature
            {
                Html = null,
                Text = null,
                IsAccessible = false,
                AccessError = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<bool> SetSignatureAsync(string userEmail, string htmlSignature, string? textSignature = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting signature for {UserEmail} via EWS", userEmail);
            _logger.LogInformation("HTML signature length: {Length} chars", htmlSignature?.Length ?? 0);

            var service = await CreateExchangeServiceAsync(userEmail, cancellationToken);

            UserConfiguration userConfig;
            try
            {
                userConfig = await UserConfiguration.Bind(
                    service,
                    "OWA.UserOptions",
                    WellKnownFolderName.Root,
                    UserConfigurationProperties.All);
                _logger.LogInformation("Bound to existing OWA.UserOptions, dictionary has {Count} items", userConfig.Dictionary.Count);
            }
            catch (ServiceResponseException ex) when (ex.ErrorCode == ServiceError.ErrorItemNotFound)
            {
                _logger.LogInformation("OWA.UserOptions not found, creating new...");
                userConfig = new UserConfiguration(service);
                await userConfig.Save("OWA.UserOptions", WellKnownFolderName.Root);

                userConfig = await UserConfiguration.Bind(
                    service,
                    "OWA.UserOptions",
                    WellKnownFolderName.Root,
                    UserConfigurationProperties.All);
                _logger.LogInformation("Created and bound to new OWA.UserOptions");
            }

            // Log existing signature-related keys before update
            _logger.LogInformation("=== Before update ===");
            foreach (var key in new[] { "signaturehtml", "signaturetext", "autoaddsignature", "autoaddsignatureonreply",
                                        "signaturename", "newsignature", "replysignature" })
            {
                if (userConfig.Dictionary.ContainsKey(key))
                {
                    var val = userConfig.Dictionary[key]?.ToString() ?? "null";
                    _logger.LogInformation("  {Key} = {Value}", key, val.Length > 100 ? val.Substring(0, 100) + "..." : val);
                }
            }

            // Set signature values using multiple key formats that OWA might use
            userConfig.Dictionary["signaturehtml"] = htmlSignature;
            userConfig.Dictionary["signaturetext"] = textSignature ?? StripHtml(htmlSignature);

            // These keys control whether signatures are automatically added
            userConfig.Dictionary["autoaddsignature"] = true;
            userConfig.Dictionary["autoaddsignatureonreply"] = true;

            // Try additional keys that newer OWA versions might use
            userConfig.Dictionary["signaturename"] = "Default";
            userConfig.Dictionary["newsignature"] = "Default";
            userConfig.Dictionary["replysignature"] = "Default";

            _logger.LogInformation("Calling Update()...");
            await userConfig.Update();
            _logger.LogInformation("Update() completed");

            // Verify by reading back
            _logger.LogInformation("Verifying write by reading back...");
            var verifyConfig = await UserConfiguration.Bind(
                service,
                "OWA.UserOptions",
                WellKnownFolderName.Root,
                UserConfigurationProperties.Dictionary);

            _logger.LogInformation("=== After update (verification) ===");
            if (verifyConfig.Dictionary.ContainsKey("signaturehtml"))
            {
                var savedSig = verifyConfig.Dictionary["signaturehtml"]?.ToString() ?? "null";
                _logger.LogInformation("  signaturehtml = {Value} ({Length} chars)",
                    savedSig.Length > 100 ? savedSig.Substring(0, 100) + "..." : savedSig,
                    savedSig.Length);
            }
            else
            {
                _logger.LogWarning("  signaturehtml key NOT FOUND after update!");
            }

            _logger.LogInformation("Successfully set signature for {UserEmail}", userEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting signature for {UserEmail}", userEmail);
            return false;
        }
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        // Simple HTML stripping - remove tags
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "").Trim();
    }
}
