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

    // Change tracking
    public bool IsDirty { get; set; } = false;
    public bool IsSaving { get; set; } = false;
    public string? SaveError { get; set; }

    // Original values for dirty checking
    public string? OriginalJobTitle { get; set; }
    public string? OriginalDepartment { get; set; }
    public string? OriginalBusinessPhone { get; set; }
    public string? OriginalMobilePhone { get; set; }

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
    /// Checks if any editable field has changed from its original value.
    /// </summary>
    public void CheckDirty()
    {
        IsDirty = JobTitle != OriginalJobTitle ||
                  Department != OriginalDepartment ||
                  BusinessPhone != OriginalBusinessPhone ||
                  MobilePhone != OriginalMobilePhone;
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
        IsDirty = false;
        SaveError = null;
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
        IsDirty = false;
        SaveError = null;
    }
}
