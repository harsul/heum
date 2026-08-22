var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("heum-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("heumdb");

var mailpit = builder.AddMailPit("mailpit");

var smtpEndpoint = mailpit.GetEndpoint("smtp");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume("heum-keycloak-data")
    .WithRealmImport("./KeycloakImport")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(mailpit)
    .WithEnvironment("KC_SMTP_HOST", smtpEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment("KC_SMTP_PORT", smtpEndpoint.Property(EndpointProperty.Port));

var messaging = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

var tenantEventsTopic = messaging.AddServiceBusTopic("tenant-events");
tenantEventsTopic.AddServiceBusSubscription("db-seeding-sub");

var userEventsTopic = messaging.AddServiceBusTopic("user-events");
userEventsTopic.AddServiceBusSubscription("user-onboarding-sub");

var keycloakAdminSecret = builder.AddParameter("KeycloakAdminSecret", secret: true);

// Applies EF Core migrations before the services that depend on the schema start.
var migrations = builder.AddProject<Projects.Heum_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

var server = builder.AddProject<Projects.Heum_Server>("server")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(mailpit)
    .WithReference(keycloak)
    .WithReference(messaging)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(mailpit)
    .WaitFor(messaging)
    .WaitForCompletion(migrations)
    .WithEnvironment("KeycloakAdmin__Realm", "saas-app")
    .WithEnvironment("KeycloakAdmin__ClientId", "tenant-provisioning-service")
    .WithEnvironment("KeycloakAdmin__ClientSecret", keycloakAdminSecret)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WithReference(keycloak)
    .WaitFor(server)
    .WaitFor(keycloak);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

// Background worker: sends the Keycloak onboarding action link (profile, password, verify
// email) when a new tenant/user is provisioned.
builder.AddAzureFunctionsProject<Projects.Heum_Functions>("useronboarding")
    .WithReference(cache)
    .WithReference(keycloak)
    .WithReference(messaging)
    .WaitFor(cache)
    .WaitFor(keycloak)
    .WaitFor(messaging)
    .WithEnvironment("KeycloakAdmin__Realm", "saas-app")
    .WithEnvironment("KeycloakAdmin__ClientId", "tenant-provisioning-service")
    .WithEnvironment("KeycloakAdmin__ClientSecret", keycloakAdminSecret)
    .WithEnvironment("KeycloakAdmin__OnboardingRedirectUri", webfrontend.GetEndpoint("http").Property(EndpointProperty.Url));

builder.Build().Run();
