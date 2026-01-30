using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public interface IGraphUserService
{
    Task<IReadOnlyList<UserProfile>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserProfile?> GetUserAsync(string userIdOrEmail, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<MailboxSignature> GetUserSignatureAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(string userId, string? jobTitle, string? department, string? businessPhone, string? mobilePhone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the profile of the currently logged-in user (delegated permissions).
    /// </summary>
    Task<UserProfile?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
