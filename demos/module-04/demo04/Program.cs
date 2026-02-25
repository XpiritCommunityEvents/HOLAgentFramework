using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>();

builder.Services.AddHostedService<Worker>();
builder.AddOpenAIClient("openai")
       .AddChatClient("gpt-5-mini");

var host = builder.Build();
await host.RunAsync();