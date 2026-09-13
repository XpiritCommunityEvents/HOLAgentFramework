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

var httpClient = new HttpClient(new LoggingHandler(new HttpClientHandler()));

httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

var requestBody = new
{
    model,
    messages = new[]
    {
        new
        {
            role = "user",
            content = "What is the weather in San Diego?"
        }
    },
    max_tokens = 500
};

using var content = new StringContent(
    JsonSerializer.Serialize(requestBody),
    Encoding.UTF8,
    "application/json");

using var response = await httpClient.PostAsync(endpoint, content);
var responseBody = await response.Content.ReadAsStringAsync();

response.EnsureSuccessStatusCode();

