using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public interface ITemplateStorageService
{
    /// <summary>
    /// Gets the current template design, or creates a default if none exists.
    /// </summary>
    Task<TemplateDesign> GetTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the template design to storage.
    /// </summary>
    Task SaveTemplateAsync(TemplateDesign template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available template designs.
    /// </summary>
    Task<IReadOnlyList<TemplateDesign>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template by ID.
    /// </summary>
    Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);
}
