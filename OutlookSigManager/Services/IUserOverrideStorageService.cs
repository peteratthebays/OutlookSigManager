using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

/// <summary>
/// Service for persisting user signature overrides to local storage.
/// </summary>
public interface IUserOverrideStorageService
{
    /// <summary>
    /// Gets the saved overrides for a user, or null if none exist.
    /// </summary>
    Task<UserOverride?> GetOverrideAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves overrides for a user.
    /// </summary>
    Task SaveOverrideAsync(UserOverride userOverride, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes overrides for a user.
    /// </summary>
    Task<bool> DeleteOverrideAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all saved overrides.
    /// </summary>
    Task<IReadOnlyList<UserOverride>> GetAllOverridesAsync(CancellationToken cancellationToken = default);
}
