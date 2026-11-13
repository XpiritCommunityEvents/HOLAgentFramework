using GloboTicket.Ordering.Services;
using GloboTicket.Ordering.MCP;
using ModelContextProtocol.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderingDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("OrderingDb")
        ?? throw new InvalidOperationException("The OrderingDb connection string is not configured.")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<OrderingService>();
builder.Services.AddHttpClient<IOrderingCatalog, OrderingCatalog>(client =>
    client.BaseAddress = new Uri(builder.Configuration["ApiConfigs:EventCatalog:Uri"]
        ?? throw new InvalidOperationException("The event catalog URI is not configured.")));
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateful)
    .WithToolsFromAssembly()
    .WithResources<OrderingContext>().WithPrompts<OrderingContext>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapMcp("/mcp");
app.MapDefaultEndpoints();

app.Run();
