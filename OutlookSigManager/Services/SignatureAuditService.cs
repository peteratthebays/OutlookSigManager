using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public class SignatureAuditService : ISignatureAuditService
{
    private readonly IGraphUserService _graphUserService;
    private readonly IExchangeSignatureService _exchangeSignatureService;
    private readonly ISignatureTemplateService _templateService;
    private readonly ILogger<SignatureAuditService> _logger;

    public SignatureAuditService(
        IGraphUserService graphUserService,
        IExchangeSignatureService exchangeSignatureService,
        ISignatureTemplateService templateService,
        ILogger<SignatureAuditService> logger)
    {
        _graphUserService = graphUserService;
        _exchangeSignatureService = exchangeSignatureService;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SignatureAuditResult>> AuditAllUsersAsync(
        IProgress<AuditProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SignatureAuditResult>();

        _logger.LogInformation("Starting signature audit for all users");

        var users = await _graphUserService.GetAllUsersAsync(cancellationToken);
        var template = _templateService.GetDefaultTemplate();

        for (int i = 0; i < users.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = users[i];

            progress?.Report(new AuditProgress
            {
                TotalUsers = users.Count,
                ProcessedUsers = i,
                CurrentUserName = user.DisplayName
            });

            var result = await AuditUserInternalAsync(user, template, cancellationToken);
            results.Add(result);
        }

        progress?.Report(new AuditProgress
        {
            TotalUsers = users.Count,
            ProcessedUsers = users.Count,
            CurrentUserName = "Complete"
        });

        _logger.LogInformation("Completed signature audit for {Count} users", results.Count);

        return results;
    }

    public async Task<SignatureAuditResult> AuditUserAsync(
        string userIdOrEmail,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Auditing single user: {UserIdOrEmail}", userIdOrEmail);

        // Try to get user - Graph accepts both ID and userPrincipalName (email)
        var user = await _graphUserService.GetUserAsync(userIdOrEmail, cancellationToken);

        // If not found by ID, try by email filter
        if (user == null && userIdOrEmail.Contains('@'))
        {
            _logger.LogInformation("User not found by ID, trying email lookup: {Email}", userIdOrEmail);
            user = await _graphUserService.GetUserByEmailAsync(userIdOrEmail, cancellationToken);
        }

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserIdOrEmail}", userIdOrEmail);
            return new SignatureAuditResult
            {
                User = new UserProfile { Id = userIdOrEmail, DisplayName = "Unknown User" },
                Status = SignatureStatus.Error,
                ErrorMessage = "User not found in Microsoft Graph"
            };
        }

        _logger.LogInformation("Found user: {DisplayName} ({Mail}), ID: {Id}", user.DisplayName, user.Mail, user.Id);

        var template = _templateService.GetDefaultTemplate();
        return await AuditUserInternalAsync(user, template, cancellationToken);
    }

    private async Task<SignatureAuditResult> AuditUserInternalAsync(
        UserProfile user,
        SignatureTemplate template,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Mail))
            {
                return new SignatureAuditResult
                {
                    User = user,
                    Status = SignatureStatus.Error,
                    ErrorMessage = "User does not have an email address"
                };
            }

            // Generate the expected signature based on user profile
            var expectedSignature = _templateService.RenderSignature(template, user);

            // Check profile completeness - these fields should be populated for a proper signature
            var discrepancies = new List<SignatureDiscrepancy>();

            if (string.IsNullOrWhiteSpace(user.JobTitle))
            {
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "JobTitle",
                    Description = "Job title is missing - signature will show empty title"
                });
            }

            if (string.IsNullOrWhiteSpace(user.Department))
            {
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "Department",
                    Description = "Department is missing - signature will show empty department"
                });
            }

            if (string.IsNullOrWhiteSpace(user.BusinessPhone) && string.IsNullOrWhiteSpace(user.MobilePhone))
            {
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "Phone",
                    Description = "No phone number set - signature will show empty phone"
                });
            }

            // Try to check mailbox access (validates EWS permissions)
            MailboxSignature? signature = null;
            try
            {
                signature = await _exchangeSignatureService.GetSignatureAsync(user.Mail, cancellationToken);

                if (!signature.IsAccessible)
                {
                    discrepancies.Add(new SignatureDiscrepancy
                    {
                        Field = "MailboxAccess",
                        Description = signature.AccessError ?? "Cannot access mailbox to deploy signature"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not verify mailbox access for {Email}: {Error}", user.Mail, ex.Message);
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "MailboxAccess",
                    Description = $"Cannot verify mailbox access: {ex.Message}"
                });
            }

            // Determine status based on profile completeness
            var status = DetermineProfileStatus(user, discrepancies, signature);

            return new SignatureAuditResult
            {
                User = user,
                Status = status,
                ExpectedSignatureHtml = expectedSignature,
                CurrentSignatureHtml = signature?.Html, // Will be null for new Outlook users
                Discrepancies = discrepancies
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auditing user {UserId}", user.Id);

            return new SignatureAuditResult
            {
                User = user,
                Status = SignatureStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    private static SignatureStatus DetermineProfileStatus(UserProfile user, List<SignatureDiscrepancy> discrepancies, MailboxSignature? signature)
    {
        // Check for mailbox access issues
        if (discrepancies.Any(d => d.Field == "MailboxAccess" && d.Description?.Contains("Cannot access") == true))
        {
            return SignatureStatus.NotAccessible;
        }

        // Check for critical missing profile data
        var hasCriticalMissing = string.IsNullOrWhiteSpace(user.JobTitle) ||
                                  string.IsNullOrWhiteSpace(user.Department);

        if (hasCriticalMissing)
        {
            return SignatureStatus.Incomplete; // Profile needs updating before signature can be deployed
        }

        // If we have a legacy signature, compare it
        if (!string.IsNullOrEmpty(signature?.Html))
        {
            return SignatureStatus.Match; // Has existing signature (legacy format)
        }

        // Profile is complete, ready for signature deployment
        return SignatureStatus.ReadyToDeploy;
    }

    public AuditSummary CalculateSummary(IReadOnlyList<SignatureAuditResult> results, TimeSpan duration)
    {
        return new AuditSummary
        {
            TotalUsers = results.Count,
            CompliantUsers = results.Count(r => r.Status == SignatureStatus.Match),
            ReadyToDeployCount = results.Count(r => r.Status == SignatureStatus.ReadyToDeploy),
            IncompleteProfileCount = results.Count(r => r.Status == SignatureStatus.Incomplete),
            MissingSignatures = results.Count(r => r.Status == SignatureStatus.Missing),
            OutdatedSignatures = results.Count(r => r.Status == SignatureStatus.Outdated),
            InconsistentSignatures = results.Count(r => r.Status == SignatureStatus.Inconsistent),
            ErrorCount = results.Count(r => r.Status == SignatureStatus.Error),
            NotAccessibleCount = results.Count(r => r.Status == SignatureStatus.NotAccessible),
            AuditTimestamp = DateTime.UtcNow,
            AuditDuration = duration
        };
    }
}
