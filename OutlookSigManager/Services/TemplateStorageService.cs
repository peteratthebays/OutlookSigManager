using System.Text.Json;
using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

/// <summary>
/// JSON file-based storage for email signature templates.
/// Includes schema versioning and migrations for safe upgrades.
/// </summary>
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
        _templatesDirectory = Path.Combine(GetPersistentDataDirectory(environment), "templates");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDirectoryExists();
    }

    /// <summary>
    /// Gets a persistent data directory that survives deployments.
    /// On Azure App Service, uses D:\home\data which is outside the deployment folder.
    /// Locally, uses Data folder in the content root.
    /// </summary>
    private static string GetPersistentDataDirectory(IWebHostEnvironment environment)
    {
        // Check if running on Azure App Service
        var websiteSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        if (!string.IsNullOrEmpty(websiteSiteName))
        {
            // Azure App Service - use persistent storage outside wwwroot
            return @"D:\home\data\baysig";
        }

        // Local development - use Data folder in content root
        return Path.Combine(environment.ContentRootPath, "Data");
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

                    // Run migrations if needed
                    var migrated = RunMigrations(template);

                    // Merge any missing fields from defaults (handles new fields)
                    var merged = MergeMissingFields(template);

                    if (migrated || merged)
                    {
                        _logger.LogInformation("Template was updated, saving new version");
                        await SaveTemplateAsync(template, cancellationToken);
                    }

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

    #region Schema Migrations

    /// <summary>
    /// Runs any pending migrations to bring the template up to the current schema version.
    /// Returns true if any migrations were run.
    /// </summary>
    private bool RunMigrations(TemplateDesign template)
    {
        if (template.SchemaVersion == TemplateDesign.CurrentSchemaVersion)
        {
            _logger.LogDebug("Template schema is up to date (version {Version})", template.SchemaVersion);
            return false;
        }

        if (template.SchemaVersion > TemplateDesign.CurrentSchemaVersion)
        {
            _logger.LogWarning(
                "Template schema version ({TemplateVersion}) is newer than code version ({CodeVersion}). " +
                "This may indicate a rollback. Data may be incompatible.",
                template.SchemaVersion, TemplateDesign.CurrentSchemaVersion);
            return false;
        }

        _logger.LogInformation(
            "Template schema needs upgrade: {OldVersion} → {NewVersion}",
            template.SchemaVersion, TemplateDesign.CurrentSchemaVersion);

        var migrated = false;

        // Run each migration in sequence
        for (int version = template.SchemaVersion + 1; version <= TemplateDesign.CurrentSchemaVersion; version++)
        {
            if (TemplateMigrations.TryGetValue(version, out var migration))
            {
                _logger.LogInformation("Running template migration to version {Version}: {Description}",
                    version, migration.Description);

                try
                {
                    migration.Action(template, _logger);
                    migrated = true;
                    _logger.LogInformation("Template migration to version {Version} completed successfully", version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Template migration to version {Version} failed!", version);
                    throw new InvalidOperationException(
                        $"Template migration to version {version} failed. " +
                        $"Please restore from backup or fix manually. Error: {ex.Message}", ex);
                }
            }
            else
            {
                _logger.LogDebug("No migration action for version {Version} (schema-only change)", version);
            }
        }

        // Update version
        template.SchemaVersion = TemplateDesign.CurrentSchemaVersion;

        return migrated;
    }

    /// <summary>
    /// Migration definitions. Add new migrations here when changing the schema.
    ///
    /// IMPORTANT: Never modify or remove existing migrations!
    /// Only add new migrations with the next version number.
    ///
    /// Example migration for renaming a field:
    /// { 2, ("Rename FontColor to PrimaryColor", (template, log) => {
    ///     // Migration logic here - template is the deserialized object
    ///     // For complex changes, you may need to work with JsonElement
    /// })}
    /// </summary>
    private static readonly Dictionary<int, (string Description, Action<TemplateDesign, ILogger> Action)> TemplateMigrations = new()
    {
        // Version 1 is the initial schema - no migration needed
        //
        // When you need to add a migration, follow this pattern:
        //
        // { 2, ("Description of what this migration does", (template, log) => {
        //     // ... migration logic ...
        // })},
    };

    #endregion

    /// <summary>
    /// Merges any missing fields from the default template into the loaded template
    /// and updates existing fields to match default settings (e.g., IsCustomField).
    /// This handles adding new fields when new features are added.
    /// </summary>
    private bool MergeMissingFields(TemplateDesign template)
    {
        var defaultTemplate = TemplateDesign.CreateDefault();
        var updated = false;

        foreach (var defaultField in defaultTemplate.Fields)
        {
            var existingField = template.Fields.FirstOrDefault(
                f => f.FieldId.Equals(defaultField.FieldId, StringComparison.OrdinalIgnoreCase));

            if (existingField == null)
            {
                // Add missing field
                _logger.LogInformation("Adding missing field '{FieldId}' to template", defaultField.FieldId);
                template.Fields.Add(defaultField);
                updated = true;
            }
            else
            {
                // Update IsCustomField to match default (fixes incorrectly marked fields)
                if (existingField.IsCustomField != defaultField.IsCustomField)
                {
                    _logger.LogInformation("Updating IsCustomField for '{FieldId}' from {Old} to {New}",
                        existingField.FieldId, existingField.IsCustomField, defaultField.IsCustomField);
                    existingField.IsCustomField = defaultField.IsCustomField;
                    updated = true;
                }
            }
        }

        return updated;
    }

    public async Task SaveTemplateAsync(TemplateDesign template, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        // Ensure schema version is set
        template.SchemaVersion = TemplateDesign.CurrentSchemaVersion;

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
