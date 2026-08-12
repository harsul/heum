var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("heumdb");

var server = builder.AddProject<Projects.Heum_Server>("server")
    .WithReference(cache)
    .WithReference(database)
    .WaitFor(cache)
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
