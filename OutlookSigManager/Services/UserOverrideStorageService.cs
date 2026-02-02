using LiteDB;
using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

/// <summary>
/// LiteDB-based storage for user signature overrides.
/// Includes schema versioning and migrations for safe upgrades.
/// </summary>
public class UserOverrideStorageService : IUserOverrideStorageService, IDisposable
{
    private readonly ILogger<UserOverrideStorageService> _logger;
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<UserOverride> _overrides;

    private const string CollectionName = "user_overrides";
    private const string MetadataCollectionName = "_schema_metadata";

    // ============================================================
    // SCHEMA VERSION - Increment this when you change UserOverride
    // ============================================================
    private const int CurrentSchemaVersion = 1;

    public UserOverrideStorageService(ILogger<UserOverrideStorageService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;

        var dataDirectory = GetPersistentDataDirectory(environment);
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        var databasePath = Path.Combine(dataDirectory, "user-overrides.db");
        _logger.LogInformation("Opening LiteDB database at {Path}", databasePath);

        // Use shared mode to allow concurrent access from multiple circuits/tabs
        var connectionString = $"Filename={databasePath};Connection=Shared";
        _database = new LiteDatabase(connectionString);

        // Run migrations before accessing collections
        RunMigrations();

        _overrides = _database.GetCollection<UserOverride>(CollectionName);
        // Note: No need for EnsureIndex on UserId - it's the BsonId (primary key)
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

    #region Schema Migrations

    /// <summary>
    /// Runs any pending migrations to bring the database up to the current schema version.
    /// </summary>
    private void RunMigrations()
    {
        var metadata = _database.GetCollection<SchemaMetadata>(MetadataCollectionName);
        var current = metadata.FindById("schema") ?? new SchemaMetadata { Id = "schema", Version = 0 };

        if (current.Version == CurrentSchemaVersion)
        {
            _logger.LogDebug("Database schema is up to date (version {Version})", current.Version);
            return;
        }

        if (current.Version > CurrentSchemaVersion)
        {
            _logger.LogWarning(
                "Database schema version ({DbVersion}) is newer than code version ({CodeVersion}). " +
                "This may indicate a rollback. Data may be incompatible.",
                current.Version, CurrentSchemaVersion);
            return;
        }

        _logger.LogInformation(
            "Database schema needs upgrade: {OldVersion} → {NewVersion}",
            current.Version, CurrentSchemaVersion);

        // Run each migration in sequence
        for (int version = current.Version + 1; version <= CurrentSchemaVersion; version++)
        {
            if (Migrations.TryGetValue(version, out var migration))
            {
                _logger.LogInformation("Running migration to version {Version}: {Description}",
                    version, migration.Description);

                try
                {
                    migration.Action(_database, _logger);
                    _logger.LogInformation("Migration to version {Version} completed successfully", version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Migration to version {Version} failed!", version);
                    throw new InvalidOperationException(
                        $"Database migration to version {version} failed. " +
                        $"Please restore from backup or fix manually. Error: {ex.Message}", ex);
                }
            }
            else
            {
                _logger.LogDebug("No migration action for version {Version} (schema-only change)", version);
            }
        }

        // Update stored version
        current.Version = CurrentSchemaVersion;
        current.LastMigration = DateTime.UtcNow;
        metadata.Upsert(current);

        _logger.LogInformation("Database schema upgraded to version {Version}", CurrentSchemaVersion);
    }

    /// <summary>
    /// Migration definitions. Add new migrations here when changing the schema.
    ///
    /// IMPORTANT: Never modify or remove existing migrations!
    /// Only add new migrations with the next version number.
    ///
    /// Example migration for renaming a field:
    /// { 2, ("Rename Pronouns to PreferredPronouns", (db, log) => {
    ///     var col = db.GetCollection(CollectionName);
    ///     var docs = col.FindAll().ToList();
    ///     foreach (var doc in docs)
    ///     {
    ///         if (doc.ContainsKey("Pronouns"))
    ///         {
    ///             doc["PreferredPronouns"] = doc["Pronouns"];
    ///             doc.Remove("Pronouns");
    ///             col.Update(doc);
    ///         }
    ///     }
    /// })}
    /// </summary>
    private static readonly Dictionary<int, (string Description, Action<LiteDatabase, ILogger> Action)> Migrations = new()
    {
        // Version 1 is the initial schema - no migration needed
        //
        // When you need to add a migration, follow this pattern:
        //
        // { 2, ("Description of what this migration does", (db, log) => {
        //     var col = db.GetCollection(CollectionName);
        //     // ... migration logic ...
        // })},
    };

    private class SchemaMetadata
    {
        public string Id { get; set; } = "schema";
        public int Version { get; set; }
        public DateTime? LastMigration { get; set; }
    }

    #endregion

    public Task<UserOverride?> GetOverrideAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _overrides.FindById(userId);
            if (result != null)
            {
                _logger.LogDebug("Found overrides for user {UserId}", userId);
            }
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overrides for user {UserId}", userId);
            return Task.FromResult<UserOverride?>(null);
        }
    }

    public Task SaveOverrideAsync(UserOverride userOverride, CancellationToken cancellationToken = default)
    {
        try
        {
            userOverride.LastModified = DateTime.UtcNow;

            _logger.LogInformation(
                "LiteDB Upsert for {UserId}: Dept={Dept}, Phone={Phone}, Mobile={Mobile}",
                userOverride.UserId,
                userOverride.OverrideDepartment,
                userOverride.OverrideBusinessPhone,
                userOverride.OverrideMobilePhone);

            _overrides.Upsert(userOverride);
            _database.Checkpoint();  // Force WAL to flush to main database file
            _logger.LogInformation("Saved overrides for user {UserId}", userOverride.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving overrides for user {UserId}", userOverride.UserId);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteOverrideAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = _overrides.Delete(userId);
            if (deleted)
            {
                _database.Checkpoint();  // Force WAL to flush to main database file
                _logger.LogInformation("Deleted overrides for user {UserId}", userId);
            }
            return Task.FromResult(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overrides for user {UserId}", userId);
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<UserOverride>> GetAllOverridesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var all = _overrides.FindAll().ToList();
            _logger.LogInformation("Retrieved {Count} user overrides from LiteDB", all.Count);

            foreach (var o in all)
            {
                _logger.LogInformation(
                    "LiteDB record {UserId}: Dept={Dept}, Phone={Phone}, Mobile={Mobile}",
                    o.UserId, o.OverrideDepartment, o.OverrideBusinessPhone, o.OverrideMobilePhone);
            }

            return Task.FromResult<IReadOnlyList<UserOverride>>(all);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all overrides");
            return Task.FromResult<IReadOnlyList<UserOverride>>(Array.Empty<UserOverride>());
        }
    }

    public void Dispose()
    {
        _database?.Dispose();
    }
}
