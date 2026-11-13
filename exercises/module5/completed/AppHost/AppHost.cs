using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var openAiApiKey = builder.AddParameter("openai-api-key", secret: true);
var openai = builder.AddOpenAI("openai")
    .WithApiKey(openAiApiKey)
    .WithEndpoint("https://semantic-kernel-vslive.services.ai.azure.com/openai/v1");
var model = openai.AddModel("model", "gpt-6-luna")
    .WithHealthCheck();

var sql = builder.AddSqlServer("sql").WithDataVolume();
var database = sql.AddDatabase("EventCatalogDb", "EventCatalogDb");
var orderingDatabase = sql.AddDatabase("OrderingDb", "OrderingDb");

var catalog = builder.AddProject<catalog>("catalog")
    .WithReference(database, "DefaultConnection")
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Catalog API");

var ordering = builder.AddProject<ordering>("ordering")
    .WithReference(orderingDatabase)
    .WaitFor(orderingDatabase)
    .WithEnvironment("ApiConfigs__EventCatalog__Uri", catalog.GetEndpoint("http"))
    .WaitFor(catalog)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Ordering API");

var frontend =builder.AddProject<frontend>("GloboTicketAssistant")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ApiConfigs__EventCatalog__Uri", catalog.GetEndpoint("http"))
    .WithEnvironment("ApiConfigs__Ordering__Uri", ordering.GetEndpoint("http"))
    .WithReference(model)
    .WithEnvironment("OpenAI__Endpoint", openai.Resource.Endpoint)
    .WithEnvironment("OpenAI__Model", model.Resource.Model)
    .WithEnvironment("OpenAI__ApiKey", openAiApiKey)
    .WaitFor(model)
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("http", url => url.DisplayText = "Web UI");

var devui = builder.AddDevUI("devui")
    .WithAgentService(frontend)
    .WaitFor(frontend);

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddMcpInspector("mcp-inspector", catalog, ordering);
}

builder.Build().Run();
