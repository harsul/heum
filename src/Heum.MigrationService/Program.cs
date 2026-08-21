using Heum.Data;
using Heum.Data.Auditing;
using Heum.MigrationService;
using Heum.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<ICurrentUserService, SystemCurrentUserService>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddDatabase();

var host = builder.Build();
host.Run();
