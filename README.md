# Heum

Heum is a multi-tenant SaaS platform with a .NET backend and a React frontend. Tenants are isolated at the data layer; users authenticate via Keycloak with role-based access control (system admins vs. tenant admins).

## Tech Stack

### Backend

| Technology | Purpose |
|---|---|
| .NET 10 | Runtime |
| ASP.NET Core Minimal APIs | HTTP endpoints |
| .NET Aspire | Local orchestration — wires Postgres, Redis, Keycloak, MailPit, Service Bus emulator, and all services |
| Entity Framework Core | ORM + migrations; global query filters enforce tenant isolation and soft-delete |
| PostgreSQL | Primary database |
| Redis | Caching |
| Keycloak | OIDC identity provider; realm roles drive authorization policies |
| Azure Service Bus | Async event messaging |
| Azure Functions v4 | Event consumers (e.g. user onboarding emails) |
| MailPit | Local SMTP / email preview |

### Frontend

| Technology | Purpose |
|---|---|
| React 19 | UI framework |
| TypeScript | Type safety |
| Vite | Dev server and bundler |
| MUI (Material UI) v9 | Component library |
| TanStack Query | Server state and data fetching |
| Axios | HTTP client |
| react-router-dom v7 | Client-side routing |
| react-oidc-context | OIDC session management |
| notistack | Snackbar notifications |

### Testing

| Technology | Purpose |
|---|---|
| xUnit v3 | Test framework |
| WebApplicationFactory | Integration test host |
| EF InMemory / Testcontainers | DB backend for tests (real Postgres in CI) |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- Docker (for Aspire to spin up infrastructure)

### Run the full stack

```bash
dotnet restore
dotnet run --project src/Heum.AppHost
```

Aspire starts Postgres, Redis, Keycloak, MailPit, the Service Bus emulator, the API server, and the frontend dev server automatically.

### Run the frontend standalone

```bash
cd src/frontend
npm install
npm run dev
```

### Run tests

```bash
dotnet test
```

Set `USE_TESTCONTAINERS=true` to run integration tests against a real PostgreSQL container instead of EF InMemory.
