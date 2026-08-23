using Heum.BackgroundService;
using Heum.BackgroundService.Outbox;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
// Transactional outbox: domain events are written to the OutboxMessages table in the same
// transaction as the entity change that raised them (see DomainEventDispatchingInterceptor),
// and OutboxProcessorHostedService polls that table to actually publish them to Service Bus.
builder.Services.Configure<OutboxProcessorOptions>(
    builder.Configuration.GetSection(OutboxProcessorOptions.SectionName));
builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();

var host = builder.Build();
host.Run();