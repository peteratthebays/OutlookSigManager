# Baysig (OutlookSigManager) - Development Braindump
**Date:** 2026-02-02
**Since commit:** efe396050fa00ded400712a622477a325285c79b (Initial commit)

## Overview

Baysig is a Blazor Server application for The Bays Healthcare Group that manages Outlook email signatures. It allows:
- **Regular users**: Create and copy their email signature with personal overrides
- **Admins**: Edit user profile overrides, design signature templates

## Key Architecture Decisions

### Authentication & Authorization
- **Entra ID (Azure AD)** authentication via OpenID Connect
- **Group-based authorization**: Admin group ID `f1d71bd2-1dfa-4f15-a9b8-bb3aee6f81f0`
- Policy name: `AdminOnly` - gates access to User Audit, Template Designer
- Delegated permission: `User.Read` only (not `User.Read.All` to avoid admin consent)
- Application permission: `User.Read.All` for backend Graph API calls

### Data Storage
- **LiteDB** (embedded NoSQL) for user overrides - file: `user-overrides.db`
- **JSON files** for templates - file: `templates/default-template.json`
- **Persistent storage location**:
  - Azure: `D:\home\data\baysig\` (outside deployment folder, survives redeploys)
  - Local: `Data\` folder in content root

### Why Not Entra for User Data?
Users synced from on-premises AD cannot have attributes modified via Graph API (403 errors). Solution: Store overrides locally in LiteDB and merge with Entra data at runtime.

## Files Changed from Initial Commit

### Deleted (Removed Features)
- `Components/Dashboard/` - Entire dashboard feature removed
- `Components/Pages/Dashboard.razor` - Removed
- `Components/Pages/Counter.razor` - Sample page removed
- `Components/Pages/Weather.razor` - Sample page removed

### New Files
- `Components/Pages/ClaimsDebug.razor` - Debug page showing auth claims (`/claims-debug`)
- `Components/Pages/DataDebug.razor` - Debug page showing raw database contents (`/data-debug`)
- `Models/UserOverride.cs` - LiteDB model for user signature overrides
- `Services/IUserOverrideStorageService.cs` - Interface for override storage
- `Services/UserOverrideStorageService.cs` - LiteDB implementation with migrations

### Significantly Modified
- `Program.cs` - Added auth, forwarded headers, LiteDB registration, AdminOnly policy
- `Services/TemplateStorageService.cs` - Added persistent storage path, schema migrations
- `Services/SignatureTemplateService.cs` - Added spacing logic, field groupings
- `Components/UserAudit/UserEditGrid.razor` - Saves to LiteDB instead of Entra
- `Models/EditableUserProfile.cs` - Added Pronouns, DectPhone, WorkingDays, override methods
- `Components/SignatureCreator/SignaturePreview.razor` - Added Outlook setup instructions

## Important Implementation Details

### LiteDB BsonId Issue (CRITICAL)
The `UserOverride` model MUST have `[BsonId]` on the `UserId` property:
```csharp
[BsonId]
public string UserId { get; set; }
```
Without this, LiteDB creates auto-generated `_id` fields causing:
- Duplicate key errors
- Data not persisting correctly
- Upserts creating new documents instead of updating

**If database issues occur:** Delete `user-overrides.db` and let it recreate.

### LiteDB Shared Connection Mode
For concurrent access from multiple Blazor circuits:
```csharp
var connectionString = $"Filename={databasePath};Connection=Shared";
```

### Schema Migrations
Both storage services have migration support:

**UserOverrideStorageService:**
- `CurrentSchemaVersion` constant (currently 1)
- `_schema_metadata` collection stores version
- `Migrations` dictionary for upgrade scripts

**TemplateStorageService:**
- `SchemaVersion` property on `TemplateDesign` model
- `MergeMissingFields()` auto-adds new fields from defaults
- `TemplateMigrations` dictionary for upgrade scripts

**To add a migration:**
1. Increment `CurrentSchemaVersion`
2. Add entry to `Migrations` dictionary with transformation logic

### Signature Spacing Logic
In `SignatureTemplateService.GenerateHtmlFromDesign()`:
- **Identity fields** (name, jobtitle, department): No gaps
- **First contact field**: 8px top margin (section separator)
- **Other contact fields** (phone, mobile, email, etc.): No gaps
- **Address**: No gap (condensed with contact fields)

### Azure App Service Deployment

**Persistent storage:** Data stored in `D:\home\data\baysig\` survives deployments.

**Required App Settings:**
- `AzureAd__ClientSecret` - From user secrets or Key Vault
- Standard AzureAd settings (TenantId, ClientId, etc.)

**Cookie Configuration for Azure:**
```csharp
options.NonceCookie.SameSite = SameSiteMode.None;
options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
options.CorrelationCookie.SameSite = SameSiteMode.None;
options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
```

**Forwarded Headers:** Required for proper HTTPS detection behind load balancer.

## Pages & Routes

| Route | Page | Access | Description |
|-------|------|--------|-------------|
| `/` | SignatureCreator | Authenticated | Create personal signature |
| `/user-audit` | UserAudit | AdminOnly | Edit user profile overrides |
| `/template-designer` | TemplateDesigner | AdminOnly | Design signature template |
| `/claims-debug` | ClaimsDebug | Authenticated | Debug auth claims |
| `/data-debug` | DataDebug | AdminOnly | View raw database contents |

## Data Models

### UserOverride (LiteDB)
```csharp
[BsonId] string UserId
string? OverrideJobTitle
string? OverrideDepartment
string? OverrideBusinessPhone
string? OverrideMobilePhone
string? Pronouns
string? DectPhone
string? WorkingDays
DateTime LastModified
```

### TemplateDesign (JSON)
- SchemaVersion, SignatureWidth, FontFamily, FontSize
- PrimaryColor, SecondaryColor, DividerColor
- Fields (List<TemplateField>)
- LogoBase64, LogoWidth
- BannerLogoBase64, BannerLogoWidth, BannerLogoUrl
- Address, DisclaimerText

## Known Issues & Solutions

### Issue: Login redirect loop in Azure
**Solution:** Add forwarded headers middleware, configure SameSite cookies.

### Issue: 403 when saving user to Entra
**Cause:** On-premises synced users can't be modified via Graph.
**Solution:** Use local LiteDB for overrides.

### Issue: Data disappears after redeploy
**Cause:** Data stored in deployment folder.
**Solution:** Use `D:\home\data\baysig\` path on Azure.

### Issue: LiteDB duplicate key errors
**Cause:** Missing `[BsonId]` attribute, conflicting indexes.
**Solution:** Add `[BsonId]` to UserId, remove EnsureIndex, delete old database.

## Branding

- App name: **Baysig**
- Page titles: "Page Name - Baysig"
- NavMenu shows "Baysig" text (no logo)
- Colors: Sky Blue `#3154A5`, Dark Grey `#77787B`

## Debug Tools

- `/claims-debug` - See all auth claims, verify AdminOnly policy
- `/data-debug` - View raw LiteDB contents, verify data persistence
- Kudu console: `https://<app>.scm.azurewebsites.net` - File access, logs

## Future Considerations

1. **Backups:** Set up Azure Blob Storage backup for `D:\home\data\baysig\`
2. **Schema hash check:** Auto-detect model changes that need migrations
3. **Real-time sync:** SignalR for cross-tab updates (if needed)
4. **Reduce logging:** Change App Service log level to Warning after debugging
