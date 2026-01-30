namespace OutlookSigManager.Models;

public record UserProfile
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? Mail { get; init; }
    public string? BusinessPhone { get; init; }
    public string? MobilePhone { get; init; }
}
