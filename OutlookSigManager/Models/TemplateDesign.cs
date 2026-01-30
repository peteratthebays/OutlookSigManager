namespace OutlookSigManager.Models;

public class TemplateDesign
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default Template";

    // Styling
    public string FontFamily { get; set; } = "Arial, sans-serif";
    public string FontSize { get; set; } = "10pt";
    public string PrimaryColor { get; set; } = "#0066cc";
    public string SecondaryColor { get; set; } = "#666666";

    // Fields
    public List<TemplateField> Fields { get; set; } = new();

    // Logo (beside signature)
    public string? LogoBase64 { get; set; }
    public int LogoWidth { get; set; } = 100;

    // Banner Logo (below signature, with hyperlink)
    public string? BannerLogoBase64 { get; set; }
    public int BannerLogoWidth { get; set; } = 400;
    public string? BannerLogoUrl { get; set; }

    // Disclaimer
    public string? DisclaimerText { get; set; }

    /// <summary>
    /// Creates a default template design with standard fields.
    /// </summary>
    public static TemplateDesign CreateDefault()
    {
        return new TemplateDesign
        {
            Name = "Default Template",
            Fields = new List<TemplateField>
            {
                new() { FieldId = "name", DisplayLabel = "Name", IsEnabled = true, SortOrder = 1, IsBold = true },
                new() { FieldId = "jobTitle", DisplayLabel = "Job Title", IsEnabled = true, SortOrder = 2 },
                new() { FieldId = "department", DisplayLabel = "Department", IsEnabled = true, SortOrder = 3 },
                new() { FieldId = "businessPhone", DisplayLabel = "Business Phone", IsEnabled = true, SortOrder = 4, Prefix = "P: " },
                new() { FieldId = "mobilePhone", DisplayLabel = "Mobile Phone", IsEnabled = false, SortOrder = 5, Prefix = "M: " },
                new() { FieldId = "email", DisplayLabel = "Email", IsEnabled = true, SortOrder = 6, Prefix = "E: " },
                new() { FieldId = "workingDays", DisplayLabel = "Working Days", IsEnabled = false, SortOrder = 7, IsCustomField = true, DefaultValue = "Mon-Fri" }
            }
        };
    }
}
