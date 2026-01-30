namespace OutlookSigManager.Models;

public record SignatureDiscrepancy
{
    public string Field { get; init; } = string.Empty;
    public string? ExpectedValue { get; init; }
    public string? ActualValue { get; init; }
    public string Description { get; init; } = string.Empty;
}
