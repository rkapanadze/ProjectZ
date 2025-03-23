var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .PublishAsAzurePostgresFlexibleServer();

var projectZDb = postgres.AddDatabase("projectzdb");

var apiService = builder.AddProject<Projects.ProjectZ_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(projectZDb)
    .WaitFor(postgres);

builder.AddProject<Projects.ProjectZ_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();