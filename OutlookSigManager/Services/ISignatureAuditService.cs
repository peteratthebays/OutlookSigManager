using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public interface ISignatureAuditService
{
    Task<IReadOnlyList<SignatureAuditResult>> AuditAllUsersAsync(
        IProgress<AuditProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<SignatureAuditResult> AuditUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    AuditSummary CalculateSummary(IReadOnlyList<SignatureAuditResult> results, TimeSpan duration);
}

public record AuditProgress
{
    public int TotalUsers { get; init; }
    public int ProcessedUsers { get; init; }
    public string CurrentUserName { get; init; } = string.Empty;
    public double PercentComplete => TotalUsers > 0 ? (double)ProcessedUsers / TotalUsers * 100 : 0;
}
