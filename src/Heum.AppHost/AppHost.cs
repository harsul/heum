var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("heum-postgres-data")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("heumdb");

var mailpit = builder.AddMailPit("mailpit");

var smtpEndpoint = mailpit.GetEndpoint("smtp");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume("heum-keycloak-data")
    .WithRealmImport("./Keycloak")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(mailpit)
    .WithEnvironment("KC_SMTP_HOST", smtpEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment("KC_SMTP_PORT", smtpEndpoint.Property(EndpointProperty.Port));

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

var messaging = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator()
    .ConfigureInfrastructure(infra =>
    {
        // Enable duplicate detection on all topics so the outbox MessageId actually deduplicates
        // redeliveries after a broker restart or pod crash between publish and ack.
        // Applies to provisioned Azure environments only; the local Service Bus emulator
        // does not honor this setting.
        foreach (var topic in infra.GetProvisionableResources().OfType<Azure.Provisioning.ServiceBus.ServiceBusTopic>())
        {
            topic.RequiresDuplicateDetection = true;
            topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(10);
        }
    });

var tenantEventsTopic = messaging.AddServiceBusTopic("tenant-events");
tenantEventsTopic.AddServiceBusSubscription("db-seeding-sub");

var userEventsTopic = messaging.AddServiceBusTopic("user-events");
userEventsTopic.AddServiceBusSubscription("user-onboarding-sub");
userEventsTopic.AddServiceBusSubscription("invitation-email-sub");

var keycloakAdminSecret = builder.AddParameter("KeycloakAdminSecret", secret: true);

var migrations = builder.AddProject<Projects.Heum_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

var backgroundServices = builder.AddProject<Projects.Heum_BackgroundService>("background-services")
    .WithReference(database)
    .WithReference(messaging)
    .WaitFor(database)
    .WaitFor(messaging);

var server = builder.AddProject<Projects.Heum_Server>("server")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(mailpit)
    .WithReference(keycloak)
    .WithReference(messaging)
    .WithReference(blobs)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(mailpit)
    .WaitFor(messaging)
    .WaitFor(blobs)
    .WaitForCompletion(migrations)
    .WithEnvironment("KeycloakAdmin__ClientSecret", keycloakAdminSecret)
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WithReference(keycloak)
    .WaitFor(server)
    .WaitFor(keycloak);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.AddAzureFunctionsProject<Projects.Heum_Functions>("useronboarding")
    .WithReference(cache)
    .WithReference(keycloak)
    .WithReference(messaging)
    .WithReference(mailpit)
    .WaitFor(cache)
    .WaitFor(keycloak)
    .WaitFor(messaging)
    .WaitFor(mailpit)
    .WithEnvironment("KeycloakAdmin__ClientSecret", keycloakAdminSecret)
    .WithEnvironment("KeycloakAdmin__OnboardingRedirectUri", webfrontend.GetEndpoint("http").Property(EndpointProperty.Url))
    .WithEnvironment("Smtp__Host", smtpEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment("Smtp__Port", smtpEndpoint.Property(EndpointProperty.Port))
    .WithEnvironment("Smtp__AppBaseUrl", webfrontend.GetEndpoint("http").Property(EndpointProperty.Url));

builder.Build().Run();
