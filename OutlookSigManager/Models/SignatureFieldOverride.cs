namespace OutlookSigManager.Models;

public class SignatureFieldOverride
{
    public string? OverrideName { get; set; }
    public string? OverrideJobTitle { get; set; }
    public string? OverrideDepartment { get; set; }
    public string? OverrideBusinessPhone { get; set; }
    public string? OverrideMobilePhone { get; set; }
    public string? WorkingDays { get; set; }  // e.g., "Mon-Thu"
    public string? Pronouns { get; set; }  // e.g., "He/Him", "She/Her", "They/Them"
    public string? DectPhone { get; set; }  // Internal DECT phone number

    /// <summary>
    /// Field IDs the user has explicitly chosen to hide from their signature,
    /// even when the field has a value in Entra.
    /// </summary>
    public HashSet<string> HiddenFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Applies overrides to a user profile, returning effective values.
    /// Hidden fields are set to null so the renderer skips them.
    /// </summary>
    public UserProfile ApplyToProfile(UserProfile baseProfile)
    {
        return baseProfile with
        {
            DisplayName = !string.IsNullOrWhiteSpace(OverrideName) ? OverrideName : baseProfile.DisplayName,
            JobTitle = HiddenFields.Contains("jobTitle") ? null
                : !string.IsNullOrWhiteSpace(OverrideJobTitle) ? OverrideJobTitle : baseProfile.JobTitle,
            Department = HiddenFields.Contains("department") ? null
                : !string.IsNullOrWhiteSpace(OverrideDepartment) ? OverrideDepartment : baseProfile.Department,
            BusinessPhone = HiddenFields.Contains("businessPhone") ? null
                : !string.IsNullOrWhiteSpace(OverrideBusinessPhone) ? OverrideBusinessPhone : baseProfile.BusinessPhone,
            MobilePhone = HiddenFields.Contains("mobilePhone") ? null
                : !string.IsNullOrWhiteSpace(OverrideMobilePhone) ? OverrideMobilePhone : baseProfile.MobilePhone
        };
    }

    /// <summary>
    /// Returns true if any override has a value or any fields are hidden.
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
               HiddenFields.Count > 0;
    }

    /// <summary>
    /// Clears all override values and hidden fields.
    /// </summary>
    public void Clear()
    {
        OverrideName = null;
        OverrideJobTitle = null;
        OverrideDepartment = null;
        OverrideBusinessPhone = null;
        OverrideMobilePhone = null;
        WorkingDays = null;
        Pronouns = null;
        DectPhone = null;
        HiddenFields.Clear();
    }

    /// <summary>
    /// Normalizes pronouns by trimming whitespace and standardizing format.
    /// e.g., "  he / him  " becomes "He/Him"
    /// </summary>
    public static string? NormalizePronouns(string? pronouns)
    {
        if (string.IsNullOrWhiteSpace(pronouns))
            return null;

        // Remove leading/trailing whitespace
        var cleaned = pronouns.Trim();

        // Remove spaces around slashes: "He / Him" -> "He/Him"
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*/\s*", "/");

        // Capitalize first letter of each part: "he/him" -> "He/Him"
        var parts = cleaned.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
            }
        }

        return string.Join("/", parts);
    }
}
