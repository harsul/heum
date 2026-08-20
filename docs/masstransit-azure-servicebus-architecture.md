# MassTransit + Azure Service Bus — Production Architecture Plan

---

## How This Works in SaaS Applications

In a multi-tenant SaaS platform, domain actions (creating a tenant, onboarding a user) trigger
**integration events** that cross service boundaries. The safest pattern is:

1. **Publish-to-outbox**: the event is written to an `OutboxMessage` DB table *in the same
   transaction* as the domain change. If the DB commit fails, no event is sent. If it commits,
   the event is guaranteed to eventually reach the broker — even if the app crashes mid-flight.

2. **Background relay**: a `OutboxDeliveryService` (MassTransit-hosted) polls the outbox and
   forwards messages to **Azure Service Bus topics** (fan-out model). Each topic has one
   subscription per logical consumer (e.g. `user-onboarding-sub`).

3. **Consumer-side retry**: consumers declare an `IConsumer<T>` with an exponential retry policy.
   MassTransit intercepts exceptions and re-delivers within the consumer process before
   ever returning the message to Service Bus. After all retries fail, MassTransit dead-letters
   the message explicitly — giving full observability without silent message loss.

```
┌────────────────────────────────────────────────────────────┐
│  Heum.Server (HTTP request)                                │
│  TenantService.ProvisionTenantAsync()                      │
│    ├─ INSERT Tenant row  ──────────────────┐               │
│    ├─ IEventPublisher.PublishAsync(...)    │               │
│    │    └─ writes to OutboxMessage table  │               │
│    └─ SaveChangesAsync()  ← single commit ┘               │
│                                                            │
│  OutboxDeliveryService (background)                        │
│    └─ reads OutboxMessage → sends to Azure Service Bus     │
└────────────────────────────────────────────────────────────┘
                            │
          ┌─────────────────┴──────────────────┐
          ▼                                    ▼
  topic: tenant-events              topic: user-events
  sub:   db-seeding-sub             sub:   user-onboarding-sub
         (future consumer)                  │
                                    ┌───────┘
                                    ▼
                            Heum.Functions
                            UserOnboardingEmailConsumer
                              → Keycloak.SendRequiredActionsEmailAsync
                              → retry × 5 with exponential backoff
                              → dead-letter on exhaustion
```

---

## Context

The hand-rolled `ServiceBusEventPublisher` (built directly on `Azure.Messaging.ServiceBus` SDK)
has two production reliability gaps:

- **No transactional outbox** — if the Service Bus call fails after the DB commit, the event is
  silently lost.
- **No structured retry** — consumers must implement their own retry logic; the existing function
  rethrows and relies on raw Service Bus re-delivery with no backoff control.

MassTransit replaces both with out-of-the-box patterns. The `IEventPublisher` facade is kept
unchanged so all call sites in `TenantService` and all existing test doubles continue to work.

---

## Architecture Decision: Consumer Hosting

**Default in this plan: Azure Functions with `MassTransit.AzureFunctions`**  
Keeps scale-to-zero, uses the existing `[ServiceBusTrigger]` binding, adds `IConsumer<T>` and
the retry/dead-letter pipeline on top.

**Alternative: Worker Service**  
MassTransit natively hosts consumers in a long-running process — simpler DI, no Functions SDK,
easier to add the `db-seeding-sub` consumer later. Trade-off: always running (no scale-to-zero).
See the Worker Service Alternative section below.

---

## NuGet Package Changes

| Project | Add | Remove |
|---|---|---|
| `Heum.Data` | `MassTransit.EntityFrameworkCore 8.3.x` | — |
| `Heum.Infrastructure` | `MassTransit 8.3.x`, `MassTransit.Azure.ServiceBus.Core 8.3.x` | `Azure.Messaging.ServiceBus 7.20.1` |
| `Heum.Server` | — | `Aspire.Azure.Messaging.ServiceBus 13.4.6` |
| `Heum.Functions` | `MassTransit 8.3.x`, `MassTransit.AzureFunctions 8.3.x` | — |
| Test projects | — (no changes) | — |

---

## Files to Delete

- `src/Heum.Infrastructure/Messaging/EventTopicRegistry.cs` — replaced by MassTransit topic pinning
- `src/Heum.Infrastructure/Messaging/ServiceBusEventPublisher.cs` — replaced by `MassTransitEventPublisher`

---

## Files to Create

### `src/Heum.Infrastructure/Messaging/MassTransitEventPublisher.cs`

```csharp
using MassTransit;

namespace Heum.Infrastructure.Messaging;

internal sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : notnull
        => publishEndpoint.Publish(@event, cancellationToken);
}
```

### `src/Heum.Functions/Consumers/UserOnboardingEmailConsumer.cs`

```csharp
using Heum.Contracts.Events;
using Heum.Infrastructure.Keycloak.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Heum.Functions.Consumers;

public sealed class UserOnboardingEmailConsumer(
    IKeycloakService keycloakService,
    ILogger<UserOnboardingEmailConsumer> logger) : IConsumer<UserOnboardingRequestedEvent>
{
    public async Task Consume(ConsumeContext<UserOnboardingRequestedEvent> context)
    {
        var @event = context.Message;

        if (string.IsNullOrWhiteSpace(@event.KeycloakUserId))
        {
            logger.LogError("Message {MessageId} missing KeycloakUserId; skipping.", context.MessageId);
            return;
        }

        await keycloakService.SendRequiredActionsEmailAsync(
            @event.KeycloakUserId, ["UPDATE_PASSWORD"], context.CancellationToken);
    }
}
```

### `src/Heum.Functions/Consumers/UserOnboardingEmailConsumerDefinition.cs`

```csharp
using MassTransit;

namespace Heum.Functions.Consumers;

public sealed class UserOnboardingEmailConsumerDefinition
    : ConsumerDefinition<UserOnboardingEmailConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UserOnboardingEmailConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Retry: 5 attempts with exponential backoff (5s → 15s → 35s → 75s → ~2min)
        endpointConfigurator.UseMessageRetry(r =>
            r.Exponential(5, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(10)));
    }
}
```

---

## Files to Modify

### `src/Heum.Infrastructure/Messaging/MessagingExtensions.cs`

Replace `AddEventPublishing` with `AddMassTransitMessaging`. The `Action<IBusRegistrationConfigurator>`
callback is a seam for the caller (Heum.Server) to inject the outbox without Infrastructure
depending on `HeumDbContext`.

Topic names are pinned via `cfg.Message<T>(m => m.SetEntityName(...))` to match the
Aspire-provisioned topology — MassTransit would otherwise derive names from CLR types.

```csharp
using Heum.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heum.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public static TBuilder AddMassTransitMessaging<TBuilder>(
        this TBuilder builder,
        Action<IBusRegistrationConfigurator>? configureBus = null)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                cfg.Host(
                    builder.Configuration.GetConnectionString("messaging")
                    ?? throw new InvalidOperationException(
                        "Azure Service Bus connection string 'messaging' is not configured."));

                cfg.Message<TenantCreatedEvent>(m => m.SetEntityName("tenant-events"));
                cfg.Message<UserOnboardingRequestedEvent>(m => m.SetEntityName("user-events"));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        // Registered last so test doubles can still replace via RemoveAll<IEventPublisher>().
        builder.Services.AddTransient<IEventPublisher, MassTransitEventPublisher>();

        return builder;
    }
}
```

### `src/Heum.Data/HeumDbContext.cs` — add outbox entities

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeumDbContext).Assembly);

    // MassTransit transactional outbox tables:
    modelBuilder.AddInboxStateEntity();    // idempotent consumer deduplication
    modelBuilder.AddOutboxMessageEntity(); // pending outbound messages
    modelBuilder.AddOutboxStateEntity();   // delivery tracking / locking
}
```

### `src/Heum.Data/DataExtensions.cs` — add `AddDatabaseOutbox` helper

```csharp
public static IBusRegistrationConfigurator AddDatabaseOutbox(
    this IBusRegistrationConfigurator configurator)
{
    configurator.AddEntityFrameworkOutbox<HeumDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();                            // all Publish/Send go through outbox by default
        o.QueryDelay = TimeSpan.FromSeconds(1);      // relay poll interval
    });
    return configurator;
}
```

### `src/Heum.Server/Program.cs`

Remove `builder.AddAzureServiceBusClient("messaging")` and the old `AddEventPublishing(...)` call.
Replace with:

```csharp
// Testing env: WAF replaces IEventPublisher; in-memory bus avoids ASB connection attempts.
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddMassTransit(x => x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx)));
    builder.Services.AddTransient<IEventPublisher, MassTransitEventPublisher>();
}
else
{
    builder.AddMassTransitMessaging(bus => bus.AddDatabaseOutbox());
}
```

### `src/Heum.Server/Services/TenantService.cs`

Key change: **single `SaveChangesAsync` commits both domain rows and outbox messages atomically**.
The old double-commit pattern disappears. `AddTenantUserAsync` also gains a `SaveChangesAsync`
call (was missing, meaning events would never be delivered with the outbox).

Both `PublishAsync` calls remain unchanged. The outbox intercepts them transparently.

### `src/Heum.Functions/Program.cs`

```csharp
builder.Services.AddMassTransitForAzureFunctions(x =>
{
    x.AddConsumer<UserOnboardingEmailConsumer, UserOnboardingEmailConsumerDefinition>();
});
```

### `src/Heum.Functions/UserOnboardingEmailFunction.cs`

```csharp
public sealed class UserOnboardingEmailFunction(IMessageReceiver receiver)
{
    [Function(nameof(UserOnboardingEmailFunction))]
    public Task RunAsync(
        [ServiceBusTrigger("user-events", "user-onboarding-sub", Connection = "messaging")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken)
        => receiver.HandleConsumer<UserOnboardingEmailConsumer>(
            "user-onboarding-sub", message, messageActions, cancellationToken);
}
```

### `src/Heum.Functions/host.json` — disable auto-complete

MassTransit calls `CompleteMessageAsync` / `DeadLetterMessageAsync` itself:

```json
{
  "version": "2.0",
  "extensions": {
    "serviceBus": {
      "autoCompleteMessages": false,
      "maxAutoLockRenewalDuration": "00:30:00"
    }
  }
}
```

---

## EF Core Migration

After modifying `HeumDbContext.OnModelCreating`:

```bash
dotnet ef migrations add AddMassTransitOutbox \
  --project src/Heum.Data \
  --startup-project src/Heum.Server \
  --output-dir Migrations
```

Creates three tables: `OutboxMessage`, `OutboxState`, `InboxState`.
`Heum.MigrationService` applies the migration automatically on next startup.

---

## Aspire AppHost — No Changes

Existing topics (`tenant-events`, `user-events`) and subscriptions remain unchanged.
MassTransit reads `ConnectionStrings__messaging` injected by Aspire — same value
that `AddAzureServiceBusClient` used, just read directly via `IConfiguration`.

---

## Test Strategy — No Changes to Existing Test Files

- **Unit tests**: `FakeEventPublisher` and `TenantServiceTests` are unaffected. `TenantService`
  is still constructed manually with the fake injected as `IEventPublisher`.
- **Integration tests**: `IntegrationFixture` detects `"Testing"` environment → in-memory bus →
  `RemoveAll<IEventPublisher>()` + `FakeEvents` replace `MassTransitEventPublisher` before
  any request runs. All existing event assertions hold.

---

## Message Format — Breaking Change & Cutover

MassTransit publishes a JSON envelope (`{ "messageType": [...], "message": {...} }`) instead
of the raw JSON body the existing function reads today. Deploy both sides simultaneously:

1. Drain `user-onboarding-sub` (stop Functions, wait for in-flight messages to expire/settle).
2. Deploy `Heum.Functions` with new MassTransit consumer.
3. Deploy `Heum.Server` with MassTransit publisher + outbox.
4. Verify messages flow end-to-end via Application Insights / Service Bus metrics.

---

## Worker Service Alternative (for future `db-seeding-sub` consumer)

If the team wants to drop Azure Functions hosting:

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserOnboardingEmailConsumer, UserOnboardingEmailConsumerDefinition>();

    x.UsingAzureServiceBus((ctx, cfg) =>
    {
        cfg.Host(connectionString);
        cfg.Message<UserOnboardingRequestedEvent>(m => m.SetEntityName("user-events"));
        cfg.SubscriptionEndpoint<UserOnboardingRequestedEvent>(
            "user-onboarding-sub",
            e => e.ConfigureConsumer<UserOnboardingEmailConsumer>(ctx));
    });
});
```

Both `db-seeding-sub` and `user-onboarding-sub` consumers can live in the same Worker Service
process with separate `SubscriptionEndpoint` calls.

---

## Implementation Order

| # | Step | Risk |
|---|---|---|
| 1 | Add/remove NuGet packages in all `.csproj` files | Low |
| 2 | Delete `EventTopicRegistry.cs` + `ServiceBusEventPublisher.cs` | Low |
| 3 | Create `MassTransitEventPublisher.cs` | Low |
| 4 | Update `MessagingExtensions.cs` | Low |
| 5 | Add outbox entities to `HeumDbContext` + `AddDatabaseOutbox` helper | Low |
| 6 | Run EF migration, review generated SQL | Low |
| 7 | Update `Heum.Server/Program.cs` | Medium |
| 8 | Restructure `TenantService` (single-commit) — run unit tests after | Medium |
| 9 | Add `UserOnboardingEmailConsumer` + `ConsumerDefinition` | Low |
| 10 | Replace `UserOnboardingEmailFunction.cs` + update Functions `Program.cs` | Medium |
| 11 | Update `host.json` (`autoCompleteMessages: false`) | Low |
| 12 | Run full test suite | — |
| 13 | Drain queues → deploy Functions → deploy Server | High (prod) |
