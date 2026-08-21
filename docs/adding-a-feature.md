# Adding a Feature

This guide describes how to add a new feature to the Heum SaaS template.

## Feature Folder Structure

Every feature lives in its own folder under `src/Heum.Server/Features/`:

```
Features/{FeatureName}/
    {FeatureName}Endpoints.cs      -- Minimal API route mapping
    {FeatureName}Service.cs        -- Business logic
    I{FeatureName}Service.cs       -- Interface (for DI and testing)
    {FeatureName}Problems.cs       -- Static ProblemDetails factory methods
    {FeatureName}Mapper.cs         -- Static mapping methods (if needed)
    Models/
        {Action}Request.cs         -- Request DTOs with DataAnnotations
        {Action}Response.cs        -- Response DTOs
```

## Step by Step

### 1. Create the feature folder

```
src/Heum.Server/Features/Settings/
```

### 2. Define request/response models in `Models/`

Use DataAnnotations for validation (no FluentValidation needed):

```csharp
public class UpdateSettingsRequest
{
    [Required, StringLength(500)]
    public string Value { get; set; } = string.Empty;
}
```

### 3. Create the service interface and implementation

```csharp
public interface ISettingsService
{
    Task<TenantSettings?> GetAsync(Guid tenantId, CancellationToken ct);
    Task UpdateAsync(Guid tenantId, string value, CancellationToken ct);
}
```

Register it in `Program.cs`:

```csharp
builder.Services.AddScoped<ISettingsService, SettingsService>();
```

### 4. Create endpoints

Use Minimal APIs with typed results:

```csharp
public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder group)
    {
        var settings = group.MapGroup("/settings");
        settings.MapGet("/", GetSettingsAsync).WithName("GetSettings");
        return group;
    }
}
```

Wire them up in `Program.cs`:

```csharp
api.MapSettingsEndpoints();
```

### 5. Create a Problems class for error responses

```csharp
internal static class SettingsProblems
{
    public static ProblemDetails NotConfigured() => new()
    {
        Title = "Settings not configured",
        Status = StatusCodes.Status404NotFound,
    };
}
```

### 6. Add a mapper if needed

Only create a mapper class if you need to transform between domain entities and response DTOs. Use static methods:

```csharp
internal static class SettingsMapper
{
    public static SettingsResponse ToResponse(TenantSettings settings) => new() { ... };
}
```

## Key Conventions

- **No MediatR / CQRS** -- services are injected directly into endpoint methods
- **No AutoMapper** -- use static mapper methods
- **DataAnnotations** for request validation (activated by `AddValidation()`)
- **Typed results** (`Results<Ok<T>, NotFound, ...>`) for explicit HTTP response documentation
- **Manual DI registration** in `Program.cs`
- **ITenantContext** for self-service endpoints that need the caller's tenant ID
- **ITenantEntity** marker interface on entities that are tenant-scoped (gets automatic query filters)
- **ISoftDeletable** on entities that should be soft-deleted instead of hard-deleted
- **TimeProvider** instead of `DateTime.UtcNow` for testability

## Testing

- Add integration tests in `tests/Heum.Server.xIntegration/Tests/`
- Use `IntegrationFixture` with `ClientScope` for auth
- Create a Refit interface in `tests/Heum.Server.xIntegration/Clients/` mirroring your endpoints
- Use hand-written fakes (no Moq) for external services
