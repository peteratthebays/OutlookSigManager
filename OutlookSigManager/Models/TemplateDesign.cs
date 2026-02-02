namespace OutlookSigManager.Models;

public class TemplateDesign
{
    // ============================================================
    // SCHEMA VERSION - Increment this when you change TemplateDesign
    // ============================================================
    public const int CurrentSchemaVersion = 1;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default Template";
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    // Layout
    public int SignatureWidth { get; set; } = 400;

    // Styling (The Bays Healthcare Group branding)
    public string FontFamily { get; set; } = "Roboto, Calibri, sans-serif";
    public string FontSize { get; set; } = "10pt";
    public string PrimaryColor { get; set; } = "#3154A5";  // Sky Blue - corporate primary
    public string SecondaryColor { get; set; } = "#77787B";  // Dark Grey - body text
    public string DividerColor { get; set; } = "#3154A5";  // Sky Blue - dividing line between logo and content

    // Fields
    public List<TemplateField> Fields { get; set; } = new();

    // Logo (beside signature)
    public string? LogoBase64 { get; set; }
    public int LogoWidth { get; set; } = 100;

    // Banner Logo (below signature, with hyperlink)
    public string? BannerLogoBase64 { get; set; }
    public int BannerLogoWidth { get; set; } = 400;
    public string? BannerLogoUrl { get; set; }

    // Address (appears as last line of signature content)
    public string? Address { get; set; } = "The Bays Hospital. Vale Street, Mornington VIC 3931";

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
                new() { FieldId = "dectPhone", DisplayLabel = "DECT Phone", IsEnabled = false, SortOrder = 5 },
                new() { FieldId = "mobilePhone", DisplayLabel = "Mobile Phone", IsEnabled = false, SortOrder = 6, Prefix = "M: " },
                new() { FieldId = "email", DisplayLabel = "Email", IsEnabled = true, SortOrder = 7, Prefix = "E: " },
                new() { FieldId = "workingDays", DisplayLabel = "Working Days", IsEnabled = false, SortOrder = 8 }
            }
        };
    }
}
