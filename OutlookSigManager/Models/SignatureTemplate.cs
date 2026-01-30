namespace OutlookSigManager.Models;

public record SignatureTemplate
{
    public string Name { get; init; } = string.Empty;
    public string HtmlTemplate { get; init; } = string.Empty;
    public string? TextTemplate { get; init; }
    public bool IsDefault { get; init; }
}
