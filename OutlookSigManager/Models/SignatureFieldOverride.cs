namespace OutlookSigManager.Models;

public class SignatureFieldOverride
{
    public string? OverrideName { get; set; }
    public string? OverrideJobTitle { get; set; }
    public string? OverrideDepartment { get; set; }
    public string? OverrideBusinessPhone { get; set; }
    public string? OverrideMobilePhone { get; set; }
    public string? WorkingDays { get; set; }  // e.g., "Mon-Thu"

    /// <summary>
    /// Applies overrides to a user profile, returning effective values.
    /// </summary>
    public UserProfile ApplyToProfile(UserProfile baseProfile)
    {
        return baseProfile with
        {
            DisplayName = !string.IsNullOrWhiteSpace(OverrideName) ? OverrideName : baseProfile.DisplayName,
            JobTitle = !string.IsNullOrWhiteSpace(OverrideJobTitle) ? OverrideJobTitle : baseProfile.JobTitle,
            Department = !string.IsNullOrWhiteSpace(OverrideDepartment) ? OverrideDepartment : baseProfile.Department,
            BusinessPhone = !string.IsNullOrWhiteSpace(OverrideBusinessPhone) ? OverrideBusinessPhone : baseProfile.BusinessPhone,
            MobilePhone = !string.IsNullOrWhiteSpace(OverrideMobilePhone) ? OverrideMobilePhone : baseProfile.MobilePhone
        };
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
               !string.IsNullOrWhiteSpace(WorkingDays);
    }

    /// <summary>
    /// Clears all override values.
    /// </summary>
    public void Clear()
    {
        OverrideName = null;
        OverrideJobTitle = null;
        OverrideDepartment = null;
        OverrideBusinessPhone = null;
        OverrideMobilePhone = null;
        WorkingDays = null;
    }
}
