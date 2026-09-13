using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = configuration["OpenAI:Model"] ?? throw new InvalidOperationException("Set OpenAI:Model in appsettings.json or your environment.");
var endpoint = configuration["OpenAI:Endpoint"] ?? throw new InvalidOperationException("Set OpenAI:Endpoint in appsettings.json or your environment.");
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Set OpenAI:ApiKey in appsettings.json or your environment.");

Console.WriteLine($"Model: {model}");
Console.WriteLine($"Endpoint: {endpoint}");
