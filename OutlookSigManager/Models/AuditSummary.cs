namespace OutlookSigManager.Models;

public record AuditSummary
{
    public int TotalUsers { get; init; }
    public int CompliantUsers { get; init; }        // Has matching signature (legacy format)
    public int ReadyToDeployCount { get; init; }    // Profile complete, can deploy signature
    public int IncompleteProfileCount { get; init; } // Missing job title, department, etc.
    public int MissingSignatures { get; init; }
    public int OutdatedSignatures { get; init; }
    public int InconsistentSignatures { get; init; }
    public int ErrorCount { get; init; }
    public int NotAccessibleCount { get; init; }
    public DateTime AuditTimestamp { get; init; }
    public TimeSpan AuditDuration { get; init; }

    // Users with complete profiles (ready to deploy or already compliant)
    public int ProfileCompleteCount => CompliantUsers + ReadyToDeployCount;

    public double ProfileCompliancePercentage => TotalUsers > 0
        ? Math.Round((double)ProfileCompleteCount / TotalUsers * 100, 1)
        : 0;
}
