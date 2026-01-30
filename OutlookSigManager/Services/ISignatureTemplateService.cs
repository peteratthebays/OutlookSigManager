using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public interface ISignatureTemplateService
{
    SignatureTemplate GetDefaultTemplate();
    string RenderSignature(SignatureTemplate template, UserProfile user);
    List<SignatureDiscrepancy> CompareSignatures(string expected, string? actual);

    /// <summary>
    /// Generates HTML signature from a TemplateDesign and user profile.
    /// </summary>
    string GenerateHtmlFromDesign(TemplateDesign design, UserProfile user, SignatureFieldOverride? overrides = null);

    /// <summary>
    /// Renders a signature using the stored template design and user profile.
    /// </summary>
    Task<string> RenderSignatureFromDesignAsync(UserProfile user, SignatureFieldOverride? overrides = null, CancellationToken cancellationToken = default);
}
