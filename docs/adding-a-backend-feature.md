# Adding a backend feature

A quick reference for adding a new vertical slice to the API.
Each feature lives under `src/Heum.Server/Features/<Feature>/` and owns its endpoints, service interface, service implementation, DTOs, and problem helpers.

## File structure

```
src/
  Heum.Contracts/Events/     # Domain event contracts (cross-project boundary)
  Heum.Data/
    Models/                  # EF entities
    Migrations/              # EF migrations (generated, never hand-edited)
  Heum.Infrastructure/
    Messaging/EventTopicMap.cs  # Maps domain events → Service Bus topics
  Heum.Server/Features/<Feature>/
    Endpoints/               # (optional subfolder for admin vs. tenant endpoints)
    Models/                  # Request and response DTOs
    Services/
      I<Feature>Service.cs   # Interface
      <Feature>Service.cs    # Implementation
    <Feature>Endpoints.cs    # Minimal API route registrations
    <Feature>Problems.cs     # Typed ProblemDetails factory methods
```

## 1. Add the entity (Heum.Data)

Entities that raise domain events extend `AggregateRoot`. Encapsulate state changes in named methods that call `AddDomainEvent`:

```csharp
// src/Heum.Data/Models/Widget.cs
public sealed class Widget : AggregateRoot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Widget() { }

    public static Widget Create(Guid tenantId, string name, TimeProvider clock)
    {
        var widget = new Widget { TenantId = tenantId, Name = name, CreatedAtUtc = clock.GetUtcNow() };
        widget.AddDomainEvent(new WidgetCreatedEvent(widget.Id, tenantId));
        return widget;
    }
}
```

Register it in `HeumDbContext` (`src/Heum.Data/HeumDbContext.cs`) and add a migration:

```bash
dotnet ef migrations add AddWidgets \
  --project src/Heum.Data \
  --startup-project src/Heum.MigrationService
```

## 2. Add a domain event (optional)

If the feature raises events that cross a service boundary, add the event to `Heum.Contracts`:

```csharp
// src/Heum.Contracts/Events/WidgetCreatedEvent.cs
public sealed record WidgetCreatedEvent(Guid WidgetId, Guid TenantId) : IDomainEvent;
```

Then map it to a Service Bus topic in `EventTopicMap.MapDomainEvents()` — the `EventTopicMapTests` test will fail the build if you forget:

```csharp
// src/Heum.Infrastructure/Messaging/EventTopicMap.cs
.MapTopic<WidgetCreatedEvent>(TenantEvents)
```

## 3. Define the service interface

```csharp
// src/Heum.Server/Features/Widgets/Services/IWidgetService.cs
public interface IWidgetService
{
    Task<Widget> CreateAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task<(IReadOnlyList<Widget> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid tenantId, Guid widgetId, CancellationToken ct = default);
}
```

## 4. Add request/response DTOs

```csharp
// src/Heum.Server/Features/Widgets/Models/CreateWidgetRequest.cs
public sealed record CreateWidgetRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = default!;
}

// src/Heum.Server/Features/Widgets/Models/WidgetResponse.cs
public sealed record WidgetResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public DateTimeOffset CreatedAtUtc { get; init; }
}
```

## 5. Add typed ProblemDetails helpers

```csharp
// src/Heum.Server/Features/Widgets/WidgetProblems.cs
internal static class WidgetProblems
{
    public static ProblemDetails NotFound(Guid id) => new()
    {
        Title = "Widget not found",
        Detail = $"No widget with id '{id}' exists in this tenant.",
        Status = StatusCodes.Status404NotFound,
    };

    public static ProblemDetails NameTaken(string name) => new()
    {
        Title = "Name already in use",
        Detail = $"A widget named '{name}' already exists.",
        Status = StatusCodes.Status409Conflict,
    };
}
```

## 6. Add endpoints

```csharp
// src/Heum.Server/Features/Widgets/WidgetsEndpoints.cs
public static class WidgetsEndpoints
{
    public static RouteGroupBuilder MapWidgetsEndpoints(this RouteGroupBuilder group)
    {
        var widgets = group.MapGroup("/widgets").RequireAuthorization(AuthorizationPolicies.TenantAdmin);

        widgets.MapGet("/", ListWidgetsAsync).WithName("ListWidgets");
        widgets.MapPost("/", CreateWidgetAsync).WithName("CreateWidget");
        widgets.MapDelete("/{id:guid}", DeleteWidgetAsync).WithName("DeleteWidget");

        return group;
    }

    internal static async Task<Results<Ok<PagedResponse<WidgetResponse>>, BadRequest<ProblemDetails>>> ListWidgetsAsync(
        ITenantContext tenantContext,
        IWidgetService widgetService,
        CancellationToken ct,
        int page = 1, int pageSize = 25)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var (items, total) = await widgetService.ListAsync(tenantContext.TenantId, page, pageSize, ct);
        return TypedResults.Ok(new PagedResponse<WidgetResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = page, PageSize = pageSize, TotalCount = total,
        });
    }

    internal static async Task<Results<Created<WidgetResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> CreateWidgetAsync(
        ITenantContext tenantContext,
        CreateWidgetRequest request,
        IWidgetService widgetService,
        CancellationToken ct)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var widget = await widgetService.CreateAsync(tenantContext.TenantId, request.Name, ct);
        return TypedResults.Created($"/api/widgets/{widget.Id}", ToResponse(widget));
    }

    internal static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> DeleteWidgetAsync(
        Guid id,
        ITenantContext tenantContext,
        IWidgetService widgetService,
        CancellationToken ct)
    {
        if (!tenantContext.HasTenant)
            return TypedResults.BadRequest(TenantProblems.NoTenant());

        var deleted = await widgetService.DeleteAsync(tenantContext.TenantId, id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static WidgetResponse ToResponse(Widget w) => new()
    {
        Id = w.Id, Name = w.Name, CreatedAtUtc = w.CreatedAtUtc,
    };
}
```

## 7. Wire up in Program.cs

Register the service and map the endpoints — two lines, in the same spots as existing features:

```csharp
// Service registration (with the other AddScoped calls)
builder.Services.AddScoped<IWidgetService, WidgetService>();

// Route mapping (after the other api.Map... calls)
api.MapWidgetsEndpoints();
```

For admin-only endpoints, map under `admin` instead of `api`:

```csharp
admin.MapAdminWidgetsEndpoints();
```

## 8. Add integration tests

Add a Refit client interface in `tests/Heum.Server.xIntegration/Clients/`:

```csharp
public interface IWidgetsApi
{
    [Get("/api/widgets")]
    Task<IApiResponse<PagedResponse<WidgetResponse>>> ListWidgetsAsync(
        int page = 1, int pageSize = 25, CancellationToken ct = default);

    [Post("/api/widgets")]
    Task<IApiResponse<WidgetResponse>> CreateWidgetAsync(
        CreateWidgetRequest request, CancellationToken ct = default);

    [Delete("/api/widgets/{id}")]
    Task<IApiResponse> DeleteWidgetAsync(Guid id, CancellationToken ct = default);
}
```

Add a test class:

```csharp
[Collection(nameof(IntegrationCollection))]
public class WidgetsEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private Guid _tenantId;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = Tenant.Register("Widget Co", "widget-co", TimeProvider.System);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        _tenantId = tenant.Id;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateWidget_Returns201()
    {
        var api = fixture.GetClient<IWidgetsApi>(ClientScope.TenantAdmin(_tenantId));

        var response = await api.CreateWidgetAsync(
            new CreateWidgetRequest { Name = "Sprocket" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Sprocket", response.Content!.Name);
    }

    [Fact]
    public async Task ListWidgets_Returns401_ForAnonymous()
    {
        var api = fixture.GetClient<IWidgetsApi>(ClientScope.Anonymous);

        var response = await api.ListWidgetsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

## Authorization reference

| Scope | How to apply |
|---|---|
| Any authenticated user | `.RequireAuthorization()` |
| Tenant admin only | `.RequireAuthorization(AuthorizationPolicies.TenantAdmin)` |
| System admin only | `.RequireAuthorization(AuthorizationPolicies.SystemAdmin)` |
| Public | `.AllowAnonymous()` |

Role and policy name constants are in `src/Heum.Server/Security/`.
