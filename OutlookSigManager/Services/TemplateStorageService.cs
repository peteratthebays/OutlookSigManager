using System.Text.Json;
using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public class TemplateStorageService : ITemplateStorageService
{
    private readonly ILogger<TemplateStorageService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _templatesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string DefaultTemplateFileName = "default-template.json";

    public TemplateStorageService(ILogger<TemplateStorageService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _templatesDirectory = Path.Combine(_environment.ContentRootPath, "Data", "templates");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_templatesDirectory))
        {
            _logger.LogInformation("Creating templates directory: {Path}", _templatesDirectory);
            Directory.CreateDirectory(_templatesDirectory);
        }
    }

    public async Task<TemplateDesign> GetTemplateAsync(CancellationToken cancellationToken = default)
    {
        var defaultPath = Path.Combine(_templatesDirectory, DefaultTemplateFileName);

        if (File.Exists(defaultPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(defaultPath, cancellationToken);
                var template = JsonSerializer.Deserialize<TemplateDesign>(json, _jsonOptions);

                if (template != null)
                {
                    _logger.LogDebug("Loaded template from {Path}", defaultPath);
                    return template;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading template from {Path}, returning default", defaultPath);
            }
        }

        // Return default template if file doesn't exist or failed to load
        var defaultTemplate = TemplateDesign.CreateDefault();
        await SaveTemplateAsync(defaultTemplate, cancellationToken);
        return defaultTemplate;
    }

    public async Task SaveTemplateAsync(TemplateDesign template, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var fileName = template.Name == "Default Template" ? DefaultTemplateFileName : $"{template.Id}.json";
        var filePath = Path.Combine(_templatesDirectory, fileName);

        try
        {
            var json = JsonSerializer.Serialize(template, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger.LogInformation("Saved template '{Name}' to {Path}", template.Name, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving template to {Path}", filePath);
            throw;
        }
    }

    public async Task<IReadOnlyList<TemplateDesign>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = new List<TemplateDesign>();
        EnsureDirectoryExists();

        var files = Directory.GetFiles(_templatesDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var template = JsonSerializer.Deserialize<TemplateDesign>(json, _jsonOptions);

                if (template != null)
                {
                    templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load template from {Path}", file);
            }
        }

        if (templates.Count == 0)
        {
            // Create and return default template
            var defaultTemplate = TemplateDesign.CreateDefault();
            await SaveTemplateAsync(defaultTemplate, cancellationToken);
            templates.Add(defaultTemplate);
        }

        return templates.OrderBy(t => t.Name).ToList();
    }

    public async Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        var templates = await GetAllTemplatesAsync(cancellationToken);
        var template = templates.FirstOrDefault(t => t.Id == templateId);

        if (template == null)
        {
            _logger.LogWarning("Template {Id} not found for deletion", templateId);
            return false;
        }

        // Don't delete default template
        if (template.Name == "Default Template")
        {
            _logger.LogWarning("Cannot delete default template");
            return false;
        }

        var filePath = Path.Combine(_templatesDirectory, $"{templateId}.json");

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted template {Id} from {Path}", templateId, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {Id}", templateId);
                return false;
            }
        }

        return false;
    }
}
