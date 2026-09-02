# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build & Run

```bash
# Restore and build all .NET projects
dotnet restore
dotnet build

# Run the full stack via .NET Aspire (starts Postgres, Redis, Keycloak, MailPit, Service Bus emulator, server, frontend)
dotnet run --project src/Heum.AppHost
```

### Frontend (standalone)

```bash
cd src/frontend
npm run dev      # Vite dev server
npm run build    # tsc + vite production build
npm run lint     # ESLint
```

### Tests

```bash
# All tests
dotnet test

# Single test class
dotnet test --filter "FullyQualifiedName~TenantServiceTests"

# With coverage (matches CI)
dotnet-coverage collect --output ./coverage/coverage.cobertura.xml --output-format cobertura "dotnet test"
```

Integration tests default to **EF InMemory**. Set `USE_TESTCONTAINERS=true` to run against a real PostgreSQL Testcontainer instead (this happens automatically on GitHub Actions).

## Architecture

Heum is a multi-tenant SaaS platform. The backend is .NET 10 orchestrated by .NET Aspire; the frontend is a React 19 + TypeScript SPA.

### Backend Projects

| Project | Role |
|---|---|
| `Heum.AppHost` | Aspire orchestration entry point — wires all services and infrastructure |
| `Heum.Server` | ASP.NET Core Minimal API — all HTTP endpoints, auth pipeline, DI root |
| `Heum.Application` | Thin shared layer; hosts `ICurrentUserService` |
| `Heum.Data` | EF Core `HeumDbContext`, entity models, migrations, domain event infrastructure, global query filters for multitenancy and soft-delete |
| `Heum.Infrastructure` | Keycloak admin HTTP client, Azure Service Bus event publisher, outbox support |
| `Heum.Contracts` | Shared event contracts (`IDomainEvent` implementations) crossing project boundaries |
| `Heum.BackgroundService` | Hosted service that polls the outbox table and publishes events to Service Bus |
| `Heum.Functions` | Azure Functions v4; handles `user-events` Service Bus topic for user onboarding emails |
| `Heum.MigrationService` | Worker that applies EF migrations at startup; `Heum.Server` waits for it before starting |
| `Heum.ServiceDefaults` | Shared Aspire defaults — telemetry, health checks |

### API Structure

All endpoints live under `/api` with versioning via `X-Api-Version` header (default: v1).

Features follow a vertical slice layout inside `src/Heum.Server/Features/{Feature}/`:
- `Endpoints/` — static classes with `MapXxxEndpoints()` extension methods (Minimal API pattern)
- `Services/` — business logic behind `IXxxService` interfaces
- `Models/` — request/response DTOs
- `XxxResponseMapper.cs`, `XxxProblems.cs` — response mapping and typed `ProblemDetails` helpers

Two authorization policies:
- `SystemAdmin` — requires `SystemAdmin` Keycloak realm role; guards `/api/admin/**`
- `TenantAdmin` — requires `Admin` role; guards tenant-scoped endpoints

Keycloak packs realm roles into a `realm_access` claim; `KeycloakClaimsHelper.AddRealmRoleClaims` flattens these into standard `ClaimTypes.Role` claims on token validation.

### Domain Model

Entities that raise domain events extend `AggregateRoot` (`Heum.Data.Domain`). State changes are encapsulated in named methods (e.g., `Tenant.Register()`, `Tenant.Rename()`) that call `AddDomainEvent(...)`. `DomainEventDispatchingInterceptor` intercepts `SaveChanges` and writes events to the `OutboxMessages` table. `HeumDbContext` automatically applies global query filters for multitenancy (`ITenantEntity.TenantId`) and soft-delete (`ISoftDeletable.IsDeleted`).

### Frontend

React SPA in `src/frontend/`. Key structure:
- `src/pages/` — top-level route pages
- `src/features/` — feature-specific components and logic
- `src/components/` — shared UI components
- `src/lib/apiClient.ts` — Axios instance; token set via `setAccessToken()`
- `src/auth/` — OIDC context helpers and role constants

`App.tsx` defines all routes; `ProtectedRoute` enforces authentication and optional role requirements.

## Integration Test Conventions

`Heum.Server.xIntegration` uses `WebApplicationFactory<Program>` + xunit.v3.

- **`IntegrationFixture`** — shared factory; replaces real infrastructure with test doubles: `TestAuthHandler` for auth, EF InMemory (or Testcontainers) for the DB, `FakeKeycloakService`, no-op event publisher, in-memory caches, disabled rate limiter.
- **`IntegrationCollection`** — all test classes carry `[Collection(nameof(IntegrationCollection))]` to share one fixture instance.
- **Test class setup** — implement `IAsyncLifetime`; call `fixture.ResetDatabaseAsync()` in `InitializeAsync()` to clear state between tests.
- **Auth** — use `ClientScope` presets (`Anonymous`, `SystemAdmin`, `TenantAdmin(tenantId)`) when calling `fixture.GetClient<IXxxApi>(scope)`. `TestAuthHandler` reads `X-Test-Roles`, `X-Test-Tenant-Id`, and `X-Test-Subject` headers.
- **HTTP clients** — always Refit interfaces (`IAdminTenantsApi`, etc.) obtained from `fixture.GetClient<T>(scope)`.
- xunit.v3 `IAsyncLifetime` uses `ValueTask`, not `Task` — `InitializeAsync()` and `DisposeAsync()` must return `ValueTask`.
