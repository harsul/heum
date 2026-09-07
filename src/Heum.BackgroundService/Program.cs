using Heum.Application;
using Heum.BackgroundService.Outbox;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUserService, SystemCurrentUserService>();

builder.AddDatabase();

builder.AddAzureServiceBusClient("messaging");
builder.AddEventPublishing(topics => topics.MapDomainEvents());

builder.Services.AddOptions<OutboxProcessorOptions>()
    .Bind(builder.Configuration.GetSection(OutboxProcessorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();

var host = builder.Build();
host.Run();