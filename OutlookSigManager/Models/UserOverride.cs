using LiteDB;

namespace OutlookSigManager.Models;

/// <summary>
/// Persisted user override data stored in local database.
/// Keyed by Entra user ID.
/// </summary>
public class UserOverride
{
    /// <summary>
    /// The Entra/Azure AD user ID (GUID). This is the primary key in LiteDB.
    /// </summary>
    [BsonId]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Override for display name.
    /// </summary>
    public string? OverrideName { get; set; }

    /// <summary>
    /// Override for job title.
    /// </summary>
    public string? OverrideJobTitle { get; set; }

    /// <summary>
    /// Override for department.
    /// </summary>
    public string? OverrideDepartment { get; set; }

    /// <summary>
    /// Override for business phone.
    /// </summary>
    public string? OverrideBusinessPhone { get; set; }

    /// <summary>
    /// Override for mobile phone.
    /// </summary>
    public string? OverrideMobilePhone { get; set; }

    /// <summary>
    /// Working days (e.g., "Mon-Thu").
    /// </summary>
    public string? WorkingDays { get; set; }

    /// <summary>
    /// Preferred pronouns (e.g., "She/Her").
    /// </summary>
    public string? Pronouns { get; set; }

    /// <summary>
    /// Internal DECT phone number.
    /// </summary>
    public string? DectPhone { get; set; }

    /// <summary>
    /// Field IDs the user has explicitly hidden from their signature.
    /// </summary>
    public List<string> HiddenFields { get; set; } = new();

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Converts to SignatureFieldOverride for use in signature generation.
    /// </summary>
    public SignatureFieldOverride ToFieldOverride()
    {
        return new SignatureFieldOverride
        {
            OverrideName = OverrideName,
            OverrideJobTitle = OverrideJobTitle,
            OverrideDepartment = OverrideDepartment,
            OverrideBusinessPhone = OverrideBusinessPhone,
            OverrideMobilePhone = OverrideMobilePhone,
            WorkingDays = WorkingDays,
            Pronouns = Pronouns,
            DectPhone = DectPhone,
            HiddenFields = new HashSet<string>(HiddenFields ?? [], StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Updates from a SignatureFieldOverride.
    /// </summary>
    public void UpdateFrom(SignatureFieldOverride overrides)
    {
        OverrideName = overrides.OverrideName;
        OverrideJobTitle = overrides.OverrideJobTitle;
        OverrideDepartment = overrides.OverrideDepartment;
        OverrideBusinessPhone = overrides.OverrideBusinessPhone;
        OverrideMobilePhone = overrides.OverrideMobilePhone;
        WorkingDays = overrides.WorkingDays;
        Pronouns = overrides.Pronouns;
        DectPhone = overrides.DectPhone;
        HiddenFields = overrides.HiddenFields.ToList();
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if any override has a value.
    /// </summary>
    public bool HasOverrides()
    {
        return !string.IsNullOrWhiteSpace(OverrideName) ||
               !string.IsNullOrWhiteSpace(OverrideJobTitle) ||
               !string.IsNullOrWhiteSpace(OverrideDepartment) ||
               !string.IsNullOrWhiteSpace(OverrideBusinessPhone) ||
               !string.IsNullOrWhiteSpace(OverrideMobilePhone) ||
               !string.IsNullOrWhiteSpace(WorkingDays) ||
               !string.IsNullOrWhiteSpace(Pronouns) ||
               !string.IsNullOrWhiteSpace(DectPhone) ||
               (HiddenFields?.Count > 0);
    }
}
