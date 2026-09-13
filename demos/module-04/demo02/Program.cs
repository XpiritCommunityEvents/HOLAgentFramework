using Microsoft.Extensions.Configuration;
using ModuleAgent;
;
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

await new ChatWithAgent().LetAgentFindRide(config);
