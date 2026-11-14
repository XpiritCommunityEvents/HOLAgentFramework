using Microsoft.Extensions.Configuration;
using ModuleAgent;
Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure proper encoding for console output, especially for special characters.

;
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

await new ChatWithAgent().LetAgentFindRide(config);
