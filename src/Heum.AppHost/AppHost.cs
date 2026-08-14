var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("heum-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);;

var database = postgres.AddDatabase("heumdb");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume("heum-keycloak-data")
    .WithRealmImport("./KeycloakImport")
    .WithLifetime(ContainerLifetime.Persistent);

var mailpit = builder.AddMailPit("mailpit");

var keycloakAdminSecret = builder.AddParameter("KeycloakAdminSecret", secret: true);

var server = builder.AddProject<Projects.Heum_Server>("server")
    .WithReference(cache)
    .WithReference(database)
    .WithReference(mailpit)
    .WithReference(keycloak)
    .WaitFor(cache)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(mailpit)
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

builder.Build().Run();
