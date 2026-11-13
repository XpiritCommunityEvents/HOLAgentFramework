#nullable enable

using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Services.AI;
using GloboTicket.Frontend.Services.Ordering;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

AppContext.SetSwitch("OpenAI.Experimental.OpenTelemetry", true);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllersWithViews();

// note: for this demo we're using the DAPR_HTTP_PORT environment variable to decide if we're using Dapr or not
builder.Services.AddHttpClient<IEventCatalogService, EventCatalogService>((sp, c) =>
{
    c.BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["ApiConfigs:EventCatalog:Uri"]!);
});
builder.Services.AddHttpClient<IOrderSubmissionService, HttpOrderSubmissionService>((sp, c) =>
{
    c.BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["ApiConfigs:Ordering:Uri"]!);
});

builder.Services.AddSingleton<IShoppingBasketService, InMemoryShoppingBasketService>();
builder.Services.AddSingleton<Settings>();

builder.Services.AddSignalR();
builder.Services.AddAGUIServer();
#pragma warning disable MAAI001 // Pending approvals must survive between hosted agent requests.
builder.Services.AddSingleton<AgentSessionStore, InMemoryAgentSessionStore>();
#pragma warning restore MAAI001

string catalogBaseAddress = builder.Configuration["ApiConfigs:EventCatalog:Uri"]
    ?? throw new InvalidOperationException("The event catalog URI is not configured.");

await using McpClient mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new HttpClientTransportOptions
    {
        Name = "EventCatalog",
        Endpoint = new Uri($"{catalogBaseAddress.TrimEnd('/')}/mcp/")
    }));

string orderingBaseAddress = builder.Configuration["ApiConfigs:Ordering:Uri"]
    ?? throw new InvalidOperationException("The ordering URI is not configured.");
await using McpClient orderingMcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new HttpClientTransportOptions
    {
        Name = "Ordering",
        Endpoint = new Uri($"{orderingBaseAddress.TrimEnd('/')}/mcp")
    }));

IList<AITool> tools =
[
    .. await mcpClient.ListToolsAsync(),
    .. (await orderingMcpClient.ListToolsAsync())
        .Where(tool => tool.Name != "collect_demo_customer")
        .Select(tool => tool.Name == "submit_order" ? (AITool)new ApprovalRequiredAIFunction(tool) : tool)
];

builder.Services.AddSingleton<ResponsesClient>(sp =>
{
    string apiKey = builder.Configuration["OpenAI:ApiKey"]
        ?? throw new InvalidOperationException("The OpenAI API key is not configured.");
    string model = builder.Configuration["OpenAI:Model"]
        ?? throw new InvalidOperationException("The OpenAI model is not configured.");
    Uri endpoint = new(builder.Configuration["OpenAI:Endpoint"]
        ?? throw new InvalidOperationException("The OpenAI endpoint is not configured."));

    var openAIClient = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = endpoint });
    return openAIClient.GetResponsesClient();
});

builder.Services.AddSingleton<AIAgent>(services => ChatAssistant.Create(
    services.GetRequiredService<ResponsesClient>(),
    builder.Configuration["OpenAI:Model"]
        ?? throw new InvalidOperationException("The OpenAI model is not configured."),
    tools,
    services,
    enableSensitiveData: builder.Environment.IsDevelopment(),
    builder.Environment.ApplicationName));
builder.AddAIAgent(ChatAssistant.Name, (services, _) => services.GetRequiredService<AIAgent>())
    .WithInMemorySessionStore(withIsolation: false); // Trusted workshop: no authenticated user partition.
builder.Services.AddSingleton<ConversationStore>();

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
builder.Services.AddDevUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.MapOpenAIResponses();
app.MapOpenAIConversations();
if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();


app.MapHub<ChatHub>("/chatHub");
app.MapAGUIServer("/ag-ui", AgUiAgent.WithSessionBoundApprovals(app.Services.GetRequiredService<AIAgent>()));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=EventCatalog}/{action=Index}/{id?}");

app.MapDefaultEndpoints();

await app.RunAsync();
