using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var openAiApiKey = builder.AddParameter("openai-api-key", secret: true);
var openai = builder.AddOpenAI("openai")
    .WithApiKey(openAiApiKey)
    .WithEndpoint("https://[[foundryname]].services.ai.azure.com/openai/v1");
var chatModel = openai.AddModel("chat", "gpt-4o")
    .WithHealthCheck();

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

var frontend =builder.AddProject<frontend>("GloboTicketAssistant")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ApiConfigs__EventCatalog__Uri", catalog.GetEndpoint("http"))
    .WithEnvironment("ApiConfigs__Ordering__Uri", ordering.GetEndpoint("http"))
    .WithReference(chatModel)
    .WithEnvironment("OpenAI__Endpoint", openai.Resource.Endpoint)
    .WithEnvironment("OpenAI__Model", chatModel.Resource.Model)
    .WithEnvironment("OpenAI__ApiKey", openAiApiKey)
    .WaitFor(chatModel)
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("http", url => url.DisplayText = "Web UI");

var devui = builder.AddDevUI("devui")
    .WithAgentService(frontend)
    .WaitFor(frontend);

builder.Build().Run();
