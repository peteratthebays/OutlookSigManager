namespace OutlookSigManager.Models;

public class EditableUserProfile
{
    // All fields from UserProfile but mutable
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Mail { get; set; }
    public string? BusinessPhone { get; set; }
    public string? MobilePhone { get; set; }

    // Custom override fields (stored in local DB only)
    public string? Pronouns { get; set; }
    public string? DectPhone { get; set; }
    public string? WorkingDays { get; set; }

    // Change tracking
    public bool IsDirty { get; set; } = false;
    public bool IsSaving { get; set; } = false;
    public string? SaveError { get; set; }
    public bool HasOverride { get; set; } = false;

    // Original values for dirty checking
    public string? OriginalJobTitle { get; set; }
    public string? OriginalDepartment { get; set; }
    public string? OriginalBusinessPhone { get; set; }
    public string? OriginalMobilePhone { get; set; }
    public string? OriginalPronouns { get; set; }
    public string? OriginalDectPhone { get; set; }
    public string? OriginalWorkingDays { get; set; }

    /// <summary>
    /// Creates an editable profile from an immutable UserProfile.
    /// </summary>
    public static EditableUserProfile FromUserProfile(UserProfile user)
    {
        return new EditableUserProfile
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            JobTitle = user.JobTitle,
            Department = user.Department,
            Mail = user.Mail,
            BusinessPhone = user.BusinessPhone,
            MobilePhone = user.MobilePhone,
            OriginalJobTitle = user.JobTitle,
            OriginalDepartment = user.Department,
            OriginalBusinessPhone = user.BusinessPhone,
            OriginalMobilePhone = user.MobilePhone
        };
    }

    /// <summary>
    /// Applies saved overrides from the database.
    /// </summary>
    public void ApplyOverride(UserOverride? savedOverride)
    {
        if (savedOverride == null) return;

        HasOverride = true;

        // Apply overrides (override Entra values if set)
        if (!string.IsNullOrWhiteSpace(savedOverride.OverrideJobTitle))
            JobTitle = savedOverride.OverrideJobTitle;
        if (!string.IsNullOrWhiteSpace(savedOverride.OverrideDepartment))
            Department = savedOverride.OverrideDepartment;
        if (!string.IsNullOrWhiteSpace(savedOverride.OverrideBusinessPhone))
            BusinessPhone = savedOverride.OverrideBusinessPhone;
        if (!string.IsNullOrWhiteSpace(savedOverride.OverrideMobilePhone))
            MobilePhone = savedOverride.OverrideMobilePhone;

        Pronouns = savedOverride.Pronouns;
        DectPhone = savedOverride.DectPhone;
        WorkingDays = savedOverride.WorkingDays;

        // Update original values to match (so they don't show as dirty)
        OriginalJobTitle = JobTitle;
        OriginalDepartment = Department;
        OriginalBusinessPhone = BusinessPhone;
        OriginalMobilePhone = MobilePhone;
        OriginalPronouns = Pronouns;
        OriginalDectPhone = DectPhone;
        OriginalWorkingDays = WorkingDays;
    }

    /// <summary>
    /// Creates a UserOverride from current values.
    /// </summary>
    public UserOverride ToUserOverride()
    {
        return new UserOverride
        {
            UserId = Id,
            OverrideJobTitle = JobTitle,
            OverrideDepartment = Department,
            OverrideBusinessPhone = BusinessPhone,
            OverrideMobilePhone = MobilePhone,
            Pronouns = Pronouns,
            DectPhone = DectPhone,
            WorkingDays = WorkingDays
        };
    }

    /// <summary>
    /// Checks if any editable field has changed from its original value.
    /// </summary>
    public void CheckDirty()
    {
        IsDirty = JobTitle != OriginalJobTitle ||
                  Department != OriginalDepartment ||
                  BusinessPhone != OriginalBusinessPhone ||
                  MobilePhone != OriginalMobilePhone ||
                  Pronouns != OriginalPronouns ||
                  DectPhone != OriginalDectPhone ||
                  WorkingDays != OriginalWorkingDays;
    }

    /// <summary>
    /// Resets original values to current values after a successful save.
    /// </summary>
    public void AcceptChanges()
    {
        OriginalJobTitle = JobTitle;
        OriginalDepartment = Department;
        OriginalBusinessPhone = BusinessPhone;
        OriginalMobilePhone = MobilePhone;
        OriginalPronouns = Pronouns;
        OriginalDectPhone = DectPhone;
        OriginalWorkingDays = WorkingDays;
        IsDirty = false;
        SaveError = null;
        HasOverride = true;
    }

    /// <summary>
    /// Reverts all changes to original values.
    /// </summary>
    public void RevertChanges()
    {
        JobTitle = OriginalJobTitle;
        Department = OriginalDepartment;
        BusinessPhone = OriginalBusinessPhone;
        MobilePhone = OriginalMobilePhone;
        Pronouns = OriginalPronouns;
        DectPhone = OriginalDectPhone;
        WorkingDays = OriginalWorkingDays;
        IsDirty = false;
        SaveError = null;
    }
}
