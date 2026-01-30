using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using OutlookSigManager.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Microsoft Identity (SSO) and Graph Support
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "User.Read.All", "MailboxSettings.ReadWrite" })
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();

// Add application services
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

// Add authorization with admin policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        var adminGroupId = builder.Configuration["AdminSettings:AdminGroupId"];
        if (!string.IsNullOrEmpty(adminGroupId))
        {
            policy.RequireClaim("groups", adminGroupId);
        }
        else
        {
            // Fallback: require authenticated user (for development when no admin group is configured)
            policy.RequireAuthenticatedUser();
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
