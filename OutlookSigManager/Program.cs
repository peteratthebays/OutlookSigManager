using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using OutlookSigManager.Components;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Azure App Service (required for proper HTTPS handling behind load balancer)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Microsoft Identity (SSO) and Graph Support
// Note: High-privilege operations use Application permissions (client credentials) in GraphUserService
// User sign-in only needs basic delegated permissions
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        // Request group claims from Entra ID
        options.TokenValidationParameters.RoleClaimType = "roles";
        // Fix correlation cookie issues in Azure App Service
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    }, cookieOptions =>
    {
        // Configure cookies for production (Azure App Service)
        cookieOptions.Cookie.SameSite = SameSiteMode.None;
        cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "User.Read" })
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();

// Configure data protection for Azure App Service (persists keys for cookie encryption)
if (!builder.Environment.IsDevelopment())
{
    var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}

// Add application services
builder.Services.AddSingleton<OutlookSigManager.Services.IUserOverrideStorageService, OutlookSigManager.Services.UserOverrideStorageService>();
builder.Services.AddScoped<OutlookSigManager.Services.ITemplateStorageService, OutlookSigManager.Services.TemplateStorageService>();
builder.Services.AddScoped<OutlookSigManager.Services.IGraphUserService, OutlookSigManager.Services.GraphUserService>();
builder.Services.AddScoped<OutlookSigManager.Services.IExchangeSignatureService, OutlookSigManager.Services.EwsSignatureService>();
builder.Services.AddScoped<OutlookSigManager.Services.ISignatureTemplateService, OutlookSigManager.Services.SignatureTemplateService>();
builder.Services.AddScoped<OutlookSigManager.Services.ISignatureAuditService, OutlookSigManager.Services.SignatureAuditService>();

// Add consent handler for Blazor Server Graph API calls
builder.Services.AddScoped<MicrosoftIdentityConsentAndConditionalAccessHandler>();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddRazorPages();

// Add authorization with policies
builder.Services.AddAuthorization(options =>
{
    // Require authentication for all pages by default
    options.FallbackPolicy = options.DefaultPolicy;

    // Admin policy for User Audit, Template Designer, and Dashboard
    var adminGroupId = builder.Configuration["AdminSettings:AdminGroupId"];
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        if (!string.IsNullOrEmpty(adminGroupId))
        {
            policy.RequireClaim("groups", adminGroupId);
        }
    });
});

builder.Services.AddCascadingAuthenticationState();

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
// Forwarded headers must be first for Azure App Service
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();
app.MapRazorPages();

app.Run();
