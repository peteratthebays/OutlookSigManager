using System.Text;
using System.Text.RegularExpressions;
using OutlookSigManager.Models;

namespace OutlookSigManager.Services;

public partial class SignatureTemplateService : ISignatureTemplateService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SignatureTemplateService> _logger;
    private readonly ITemplateStorageService _templateStorage;

    private const string DefaultHtmlTemplate = """
        <table style="font-family: Arial, sans-serif; font-size: 10pt;">
          <tr>
            <td style="padding-right: 15px; border-right: 2px solid #0066cc;">
              <img src="logo.png" alt="The Bays Healthcare Group" width="100" />
            </td>
            <td style="padding-left: 15px;">
              <p style="margin: 0; font-weight: bold;">{{Name}}</p>
              <p style="margin: 0; color: #666;">{{Title}}</p>
              <p style="margin: 0; color: #666;">{{Department}}</p>
              <p style="margin: 5px 0;">P: {{Phone}} | E: {{Email}}</p>
            </td>
          </tr>
        </table>
        """;

    public SignatureTemplateService(
        IConfiguration configuration,
        ILogger<SignatureTemplateService> logger,
        ITemplateStorageService templateStorage)
    {
        _configuration = configuration;
        _logger = logger;
        _templateStorage = templateStorage;
    }

    public SignatureTemplate GetDefaultTemplate()
    {
        var templateHtml = _configuration["SignatureTemplate:Html"] ?? DefaultHtmlTemplate;
        var templateName = _configuration["SignatureTemplate:Name"] ?? "Default Corporate Template";

        return new SignatureTemplate
        {
            Name = templateName,
            HtmlTemplate = templateHtml,
            IsDefault = true
        };
    }

    public string RenderSignature(SignatureTemplate template, UserProfile user)
    {
        var result = template.HtmlTemplate;

        result = result.Replace("{{Name}}", user.DisplayName ?? string.Empty);
        result = result.Replace("{{Title}}", user.JobTitle ?? string.Empty);
        result = result.Replace("{{Department}}", user.Department ?? string.Empty);
        result = result.Replace("{{Email}}", user.Mail ?? string.Empty);
        result = result.Replace("{{Phone}}", user.BusinessPhone ?? user.MobilePhone ?? string.Empty);
        result = result.Replace("{{MobilePhone}}", user.MobilePhone ?? string.Empty);
        result = result.Replace("{{BusinessPhone}}", user.BusinessPhone ?? string.Empty);

        return result;
    }

    public List<SignatureDiscrepancy> CompareSignatures(string expected, string? actual)
    {
        var discrepancies = new List<SignatureDiscrepancy>();

        if (string.IsNullOrWhiteSpace(actual))
        {
            discrepancies.Add(new SignatureDiscrepancy
            {
                Field = "Signature",
                ExpectedValue = "Present",
                ActualValue = "Missing",
                Description = "No signature found for this user"
            });
            return discrepancies;
        }

        // Normalize both signatures for comparison
        var normalizedExpected = NormalizeHtml(expected);
        var normalizedActual = NormalizeHtml(actual);

        if (!string.Equals(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase))
        {
            // Extract and compare key fields
            var expectedName = ExtractField(expected, "font-weight: bold");
            var actualName = ExtractField(actual, "font-weight: bold");

            if (!string.Equals(expectedName, actualName, StringComparison.OrdinalIgnoreCase))
            {
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "Name",
                    ExpectedValue = expectedName,
                    ActualValue = actualName,
                    Description = "Name in signature does not match profile"
                });
            }

            // Check for email mismatch
            var expectedEmail = ExtractEmail(expected);
            var actualEmail = ExtractEmail(actual);

            if (!string.Equals(expectedEmail, actualEmail, StringComparison.OrdinalIgnoreCase))
            {
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "Email",
                    ExpectedValue = expectedEmail,
                    ActualValue = actualEmail,
                    Description = "Email in signature does not match profile"
                });
            }

            // If no specific discrepancies found but signatures differ
            if (discrepancies.Count == 0)
            {
                discrepancies.Add(new SignatureDiscrepancy
                {
                    Field = "Content",
                    ExpectedValue = "(see expected signature)",
                    ActualValue = "(see actual signature)",
                    Description = "Signature content differs from template"
                });
            }
        }

        return discrepancies;
    }

    private static string NormalizeHtml(string html)
    {
        // Remove whitespace and normalize for comparison
        var normalized = WhitespaceRegex().Replace(html, " ");
        normalized = normalized.Trim().ToLowerInvariant();
        return normalized;
    }

    private static string? ExtractField(string html, string marker)
    {
        // Simple extraction - look for content after a style marker
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        var startTag = html.IndexOf('>', index);
        if (startTag < 0) return null;

        var endTag = html.IndexOf('<', startTag);
        if (endTag < 0) return null;

        return html.Substring(startTag + 1, endTag - startTag - 1).Trim();
    }

    private static string? ExtractEmail(string html)
    {
        var match = EmailRegex().Match(html);
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();

    public string GenerateHtmlFromDesign(TemplateDesign design, UserProfile user, SignatureFieldOverride? overrides = null)
    {
        // Apply overrides if provided
        var effectiveUser = overrides != null ? overrides.ApplyToProfile(user) : user;
        var workingDays = overrides?.WorkingDays;

        var sb = new StringBuilder();

        // Start main table with configurable font
        sb.AppendLine($"<table style=\"font-family: {design.FontFamily}; font-size: {design.FontSize};\">");
        sb.AppendLine("  <tr>");

        // Logo column (if logo is present)
        if (!string.IsNullOrEmpty(design.LogoBase64))
        {
            sb.AppendLine($"    <td style=\"padding-right: 15px; border-right: 2px solid {design.PrimaryColor}; vertical-align: top;\">");
            sb.AppendLine($"      <img src=\"data:image/png;base64,{design.LogoBase64}\" alt=\"Logo\" width=\"{design.LogoWidth}\" />");
            sb.AppendLine("    </td>");
        }

        // Content column
        var contentPadding = !string.IsNullOrEmpty(design.LogoBase64) ? "padding-left: 15px;" : "";
        sb.AppendLine($"    <td style=\"{contentPadding} vertical-align: top;\">");

        // Render enabled fields in sort order
        var enabledFields = design.Fields
            .Where(f => f.IsEnabled)
            .OrderBy(f => f.SortOrder);

        foreach (var field in enabledFields)
        {
            var value = GetFieldValue(field, effectiveUser, workingDays);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var displayValue = !string.IsNullOrEmpty(field.Prefix) ? $"{field.Prefix}{value}" : value;
            var style = GetFieldStyle(field, design);

            sb.AppendLine($"      <p style=\"margin: 0; {style}\">{System.Net.WebUtility.HtmlEncode(displayValue)}</p>");
        }

        sb.AppendLine("    </td>");
        sb.AppendLine("  </tr>");

        // Disclaimer row (if present)
        if (!string.IsNullOrEmpty(design.DisclaimerText))
        {
            var colspan = !string.IsNullOrEmpty(design.LogoBase64) ? "2" : "1";
            sb.AppendLine("  <tr>");
            sb.AppendLine($"    <td colspan=\"{colspan}\" style=\"padding-top: 10px; font-size: 8pt; color: {design.SecondaryColor};\">");
            sb.AppendLine($"      {System.Net.WebUtility.HtmlEncode(design.DisclaimerText)}");
            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
        }

        // Banner logo row (if present)
        if (!string.IsNullOrEmpty(design.BannerLogoBase64))
        {
            var colspan = !string.IsNullOrEmpty(design.LogoBase64) ? "2" : "1";
            sb.AppendLine("  <tr>");
            sb.AppendLine($"    <td colspan=\"{colspan}\" style=\"padding-top: 15px;\">");

            if (!string.IsNullOrEmpty(design.BannerLogoUrl))
            {
                sb.AppendLine($"      <a href=\"{System.Net.WebUtility.HtmlEncode(design.BannerLogoUrl)}\" target=\"_blank\" style=\"text-decoration: none;\">");
            }

            sb.AppendLine($"      <img src=\"data:image/png;base64,{design.BannerLogoBase64}\" alt=\"Banner\" width=\"{design.BannerLogoWidth}\" style=\"display: block;\" />");

            if (!string.IsNullOrEmpty(design.BannerLogoUrl))
            {
                sb.AppendLine("      </a>");
            }

            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
        }

        sb.AppendLine("</table>");

        return sb.ToString();
    }

    public async Task<string> RenderSignatureFromDesignAsync(UserProfile user, SignatureFieldOverride? overrides = null, CancellationToken cancellationToken = default)
    {
        var design = await _templateStorage.GetTemplateAsync(cancellationToken);
        return GenerateHtmlFromDesign(design, user, overrides);
    }

    private static string GetFieldValue(TemplateField field, UserProfile user, string? workingDays)
    {
        return field.FieldId.ToLowerInvariant() switch
        {
            "name" => user.DisplayName,
            "jobtitle" => user.JobTitle ?? string.Empty,
            "department" => user.Department ?? string.Empty,
            "email" => user.Mail ?? string.Empty,
            "businessphone" => user.BusinessPhone ?? string.Empty,
            "mobilephone" => user.MobilePhone ?? string.Empty,
            "workingdays" => workingDays ?? field.DefaultValue ?? string.Empty,
            _ => field.DefaultValue ?? string.Empty
        };
    }

    private static string GetFieldStyle(TemplateField field, TemplateDesign design)
    {
        var styles = new List<string>();

        if (field.IsBold)
        {
            styles.Add("font-weight: bold");
        }

        // Per-field font size, or use template default
        if (!string.IsNullOrEmpty(field.FontSize))
        {
            styles.Add($"font-size: {field.FontSize}");
        }

        // Per-field color, or use template defaults (primary for name, secondary for others)
        if (!string.IsNullOrEmpty(field.Color))
        {
            styles.Add($"color: {field.Color}");
        }
        else if (field.FieldId.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            styles.Add($"color: {design.PrimaryColor}");
        }
        else
        {
            styles.Add($"color: {design.SecondaryColor}");
        }

        return string.Join("; ", styles);
    }
}
