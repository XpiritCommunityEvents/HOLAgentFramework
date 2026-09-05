#nullable enable

using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Services.AI;
using GloboTicket.Frontend.Services.Ordering;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;

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

string catalogBaseAddress = builder.Configuration["ApiConfigs:EventCatalog:Uri"]
    ?? throw new InvalidOperationException("The event catalog URI is not configured.");

await using McpClient mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new HttpClientTransportOptions
    {
        Name = "EventCatalog",
        Endpoint = new Uri($"{catalogBaseAddress.TrimEnd('/')}/mcp/")
    }));

IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

builder.Services.AddSingleton<IChatClient>(_ =>
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
    var client = openAIClient.GetChatClient(model).AsIChatClient();

    return new ChatClientBuilder(client)
        .UseOpenTelemetry(sourceName: ChatAssistant.Name, configure: (cfg) => {
            // Allows the DevUI debug panel to see actual prompt text and tool results
            cfg.EnableSensitiveData = true;
        })
        .Build();
});

builder.Services.AddSingleton<AIAgent>(services => ChatAssistant.Create(
    services.GetRequiredService<IChatClient>(),
    tools));
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=EventCatalog}/{action=Index}/{id?}");

app.MapDefaultEndpoints();

await app.RunAsync();
