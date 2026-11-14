using Microsoft.Extensions.Configuration;
using ModuleAgent;

Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure proper encoding for console output, especially for special characters.

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>();

IConfiguration config = builder.Build();

await new ChatWithAgent().LetAgentFindRideAndHotel(config);
