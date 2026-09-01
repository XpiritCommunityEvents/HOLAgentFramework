using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");
var database = sql.AddDatabase("EventCatalogDb", "EventCatalogDb");

var catalog = builder.AddProject<catalog>("catalog")
    .WithReference(database, "DefaultConnection")
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Catalog API");

var ordering = builder.AddProject<ordering>("ordering")
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Ordering API");

builder.AddProject<frontend>("frontend")
    .WithEnvironment("ApiConfigs__EventCatalog__Uri", catalog.GetEndpoint("http"))
    .WithEnvironment("ApiConfigs__Ordering__Uri", ordering.GetEndpoint("http"))
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("http", url => url.DisplayText = "Web UI");

builder.Build().Run();
