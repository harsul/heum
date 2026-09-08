using System.Text.Json.Serialization;
using Heum.Application;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Infrastructure.Keycloak;
using Heum.Infrastructure.Messaging;
using Heum.Server.Configuration;
using Heum.Server.Extensions;
using Heum.Server.Features.Dashboard.Endpoints;
using Heum.Server.Features.FeatureFlags.Endpoints;
using Heum.Server.Features.FeatureFlags.Services;
using Heum.Server.Features.Invitations;
using Heum.Server.Features.Invitations.Services;
using Heum.Server.Features.Plans.Endpoints;
using Heum.Server.Features.Plans.Services;
using Heum.Server.Features.Settings;
using Heum.Server.Features.Settings.Services;
using Heum.Server.Features.Subscriptions.Endpoints;
using Heum.Server.Features.Subscriptions.Services;
using Heum.Server.Features.Tenants;
using Heum.Server.Features.Tenants.Endpoints;
using Heum.Server.Features.Tenants.Services;
using Heum.Server.Middleware;
using Heum.Server.Security;
using Heum.Server.Services;
using Heum.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Scalar.AspNetCore;
using BlobStorageService = Heum.Server.Features.Tenants.Services.BlobStorageService;
using IBlobStorageService = Heum.Server.Features.Tenants.Services.IBlobStorageService;
using TenantService = Heum.Server.Features.Tenants.Services.TenantService;

var builder = WebApplication.CreateBuilder(args);

// Feature flags — Azure App Configuration is optional.
// When the connection string is present (provisioned via azd or user secrets), flags are stored
// in Azure App Configuration and support per-tenant/per-user targeting.
// When absent (local dev without provisioning), flags fall back to appsettings.Development.json.
var appConfigCs = builder.Configuration.GetConnectionString("appconfig");
var hasAzureAppConfig = !string.IsNullOrEmpty(appConfigCs);

if (hasAzureAppConfig)
{
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(appConfigCs!)
               .UseFeatureFlags(ff => ff.SetRefreshInterval(TimeSpan.FromMinutes(5))));

    builder.Services.AddAzureAppConfiguration();
    // Register ConfigurationClient directly from the connection string for admin CRUD operations.
    builder.Services.AddSingleton(_ => new Azure.Data.AppConfiguration.ConfigurationClient(appConfigCs));
    builder.Services.AddScoped<IFeatureFlagAdminService, AzureFeatureFlagAdminService>();
}
else
{
    builder.Services.AddScoped<IFeatureFlagAdminService, LocalFeatureFlagAdminService>();
}

builder.Services.AddFeatureManagement().AddFeatureFilter<TargetingFilter>();
builder.Services.AddScoped<ITargetingContextAccessor, HeumTargetingContextAccessor>();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddDatabase();
builder.AddKeycloakAdmin();
builder.AddRedisClientBuilder("cache")
    .WithDistributedCache();

builder.AddAzureServiceBusClient("messaging");
builder.AddEventPublishing(topics => topics.MapDomainEvents());

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: builder.Configuration["KeycloakAdmin:Realm"]!, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters.ValidateAudience = false;
        options.Events = new JwtBearerEvents
        {
            // Keycloak packs realm roles into a single "realm_access" claim instead of
            // individual role claims, so flatten it out for RequireRole/RequireAuthorization.
            OnTokenValidated = context =>
            {
                if (context.Principal is not null)
                    KeycloakClaimsHelper.AddRealmRoleClaims(context.Principal);

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.SystemAdmin, policy => policy.RequireRole(AuthorizationRoles.SystemAdmin))
    .AddPolicy(AuthorizationPolicies.TenantAdmin, policy => policy.RequireRole(AuthorizationRoles.Admin));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantStatusService, TenantStatusService>();
if (string.Equals(builder.Configuration["BlobStorage:Provider"], "Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IBlobStorageService, LocalFileBlobStorageService>();
}
else
{
    builder.AddAzureBlobServiceClient("blobs");
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
}
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPlanAdminService, PlanAdminService>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<Heum.Data.Multitenancy.ITenantProvider>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add services to the container.
// Request DTOs carry enums (e.g. CreateEntitlementRequest.Type) that the frontend sends by name
// ("Integer"); without this converter the body fails to bind and the API answers 400 with no body.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddValidation();

builder.Services.AddHeumApiVersioning();
builder.Services.AddHeumRateLimiting(builder.Configuration);
builder.Services.AddOptions<TenantRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(TenantRateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<GlobalRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(GlobalRateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Migrations are applied by the Heum.MigrationService worker before this service starts.

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.UseHeumTenantRateLimiting();
app.UseRateLimiter();
// After rate limiting so a deactivated tenant can't use the (cached) status lookup to bypass limits.
app.UseMiddleware<TenantStatusMiddleware>();
// Refreshes Azure App Configuration feature flags within the cache expiration interval.
if (hasAzureAppConfig) app.UseAzureAppConfiguration();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapVersionedApiGroup();

api.MapTenantsEndpoints();
api.MapSettingsEndpoints();
api.MapInvitationsEndpoints();
api.MapTenantEntitlementsEndpoints();
api.MapFeaturesEndpoints();

var admin = api.MapGroup("/admin").RequireAuthorization(AuthorizationPolicies.SystemAdmin);
admin.MapAdminDashboardEndpoints();
admin.MapAdminTenantsEndpoints();
admin.MapAdminPlansEndpoints();
admin.MapAdminEntitlementsEndpoints();
admin.MapAdminSubscriptionsEndpoints();
admin.MapAdminFeaturesEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
