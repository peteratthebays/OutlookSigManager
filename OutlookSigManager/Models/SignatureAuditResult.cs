namespace OutlookSigManager.Models;

public record SignatureAuditResult
{
    public UserProfile User { get; init; } = null!;
    public SignatureStatus Status { get; init; }
    public string? CurrentSignatureHtml { get; init; }
    public string? ExpectedSignatureHtml { get; init; }
    public List<SignatureDiscrepancy> Discrepancies { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public enum SignatureStatus
{
    Match,              // Has a signature that matches template (legacy format)
    Missing,            // No signature found
    Outdated,           // Signature exists but differs from template
    Inconsistent,       // Major differences in signature
    Error,              // Error during audit
    NotAccessible,      // Cannot access mailbox
    Incomplete,         // Profile data missing (title, department, etc.)
    ReadyToDeploy       // Profile complete, ready to deploy signature
}
