#nullable enable

using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Services.AI;
using GloboTicket.Frontend.Services.Ordering;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Extensions.AI;

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

// TODO: Connect the catalog MCP tools and OpenAI chat client, then register the
// Agent Framework assistant. See the completed module after attempting the exercise.
string catalogBaseAddress = builder.Configuration["ApiConfigs:EventCatalog:Uri"]
    ?? throw new InvalidOperationException("The event catalog URI is not configured.");
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
