namespace OutlookSigManager.Models;

public record MailboxSignature
{
    public string? Html { get; init; }
    public string? Text { get; init; }
    public bool IsAccessible { get; init; }
    public string? AccessError { get; init; }
}
