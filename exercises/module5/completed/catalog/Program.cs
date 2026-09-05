using GloboTicket.Catalog.DbContexts;
using GloboTicket.Catalog.MCP;
using GloboTicket.Catalog.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddDbContext<EventCatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IEventRepository, SqlEventRepository>();
builder.Services.AddScoped<CatalogTool>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventCatalogDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapMcp("/mcp");
app.MapDefaultEndpoints();

app.Run();
