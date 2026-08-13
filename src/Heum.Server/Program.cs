using Heum.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddDatabase();
builder.AddRedisClientBuilder("cache")
    .WithOutputCache();

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "saas-app", options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddProblemDetails();

var keycloakUrl = builder.Configuration.GetConnectionString("keycloak") ?? "http://localhost:8080";
var keycloakRealmUrl = $"{keycloakUrl.TrimEnd('/')}/realms/saas-app";

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["oauth2"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"{keycloakRealmUrl}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "Profile" },
                        { "email", "Email" }
                    }
                }
            }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<HeumdDbContext>()
        .Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .AddPreferredSecuritySchemes("oauth2")
            .AddOAuth2Flows("oauth2", flows =>
            {
                flows.WithPassword(password =>
                {
                    password
                        .WithClientId("react-frontend")
                        .WithTokenUrl($"{keycloakRealmUrl}/protocol/openid-connect/token");
                });
            });
    });
}

app.UseOutputCache();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

var api = app.MapGroup("/api");
api.MapGet("weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.CacheOutput(p => p.Expire(TimeSpan.FromSeconds(5)))
.WithName("GetWeatherForecast")
.RequireAuthorization();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
