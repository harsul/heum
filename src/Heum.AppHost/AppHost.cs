var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("heumdb");

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "25.0")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
    .WithVolume("keycloak-data", "/opt/keycloak/data")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithArgs("start-dev");

var mailpit = builder.AddMailPit("mailpit");

var server = builder.AddProject<Projects.Heum_Server>("server")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(mailpit)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(mailpit)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
