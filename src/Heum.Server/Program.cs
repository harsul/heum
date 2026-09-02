using Heum.Application;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Infrastructure.Keycloak;
using Heum.Infrastructure.Messaging;
using Heum.Server.Extensions;
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
using Heum.Server.Services;
using Heum.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using TenantService = Heum.Server.Features.Tenants.Services.TenantService;
using BlobStorageService = Heum.Server.Features.Tenants.Services.BlobStorageService;
using IBlobStorageService = Heum.Server.Features.Tenants.Services.IBlobStorageService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddDatabase();
builder.AddKeycloakAdmin();
builder.AddRedisClientBuilder("cache")
    .WithDistributedCache()
    .WithOutputCache();

builder.AddAzureServiceBusClient("messaging");
builder.AddAzureBlobServiceClient("blobs");
builder.AddEventPublishing(topics => topics
    .MapTopic<TenantCreatedEvent>("tenant-events")
    .MapTopic<UserOnboardingRequestedEvent>("user-events")
    .MapTopic<InvitationCreatedEvent>("user-events"));

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "saas-app", options =>
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
    .AddPolicy("SystemAdmin", policy => policy.RequireRole("SystemAdmin"))
    .AddPolicy("TenantAdmin", policy => policy.RequireRole("Admin"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
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
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddValidation();

builder.Services.AddHeumApiVersioning();
builder.Services.AddHeumRateLimiting();
builder.Services.Configure<Heum.Server.Configuration.TenantRateLimitOptions>(
    builder.Configuration.GetSection("RateLimiting:Tenant"));

var app = builder.Build();

// Migrations are applied by the Heum.MigrationService worker before this service starts.

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.UseHeumTenantRateLimiting();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseOutputCache();

var api = app.MapVersionedApiGroup();

api.MapTenantsEndpoints();
api.MapSettingsEndpoints();
api.MapInvitationsEndpoints();
api.MapTenantEntitlementsEndpoints();

var admin = api.MapGroup("/admin").RequireAuthorization("SystemAdmin");
admin.MapAdminTenantsEndpoints();
admin.MapAdminPlansEndpoints();
admin.MapAdminEntitlementsEndpoints();
admin.MapAdminSubscriptionsEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
