using Heum.Application;
using Heum.BackgroundService.Outbox;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUserService, SystemCurrentUserService>();

builder.AddDatabase();

builder.AddAzureServiceBusClient("messaging");
builder.AddEventPublishing(topics => topics
    .MapTopic<TenantCreatedEvent>("tenant-events")
    .MapTopic<UserOnboardingRequestedEvent>("user-events")
    .MapTopic<InvitationCreatedEvent>("user-events"));

builder.Services.Configure<OutboxProcessorOptions>(
    builder.Configuration.GetSection(OutboxProcessorOptions.SectionName));
builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();

var host = builder.Build();
host.Run();