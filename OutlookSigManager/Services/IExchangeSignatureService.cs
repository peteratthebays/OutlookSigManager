using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public interface IExchangeSignatureService
{
    Task<MailboxSignature> GetSignatureAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<bool> SetSignatureAsync(string userEmail, string htmlSignature, string? textSignature = null, CancellationToken cancellationToken = default);
}
