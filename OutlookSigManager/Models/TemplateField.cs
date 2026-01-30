namespace OutlookSigManager.Models;

public class TemplateField
{
    public string FieldId { get; set; } = string.Empty;  // "name", "jobTitle", etc.
    public string DisplayLabel { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Prefix { get; set; }  // e.g., "P: " for phone
    public bool IsBold { get; set; } = false;
    public bool IsCustomField { get; set; } = false;  // For "Working Days"
    public string? DefaultValue { get; set; }

    // Per-field styling (null = use template defaults)
    public string? FontSize { get; set; }
    public string? Color { get; set; }
}
