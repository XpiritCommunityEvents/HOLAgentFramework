using System.Net.Http.Headers;
using System.Text.Json;
using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = configuration["OpenAI:Model"] ?? throw new InvalidOperationException("Set OpenAI:Model in appsettings.json or your environment.");
var imageModel = configuration["OpenAI:ImageModel"] ?? throw new InvalidOperationException("Set OpenAI:ImageModel in appsettings.json or your environment.");
var endpoint = configuration["OpenAI:Endpoint"] ?? throw new InvalidOperationException("Set OpenAI:Endpoint in appsettings.json or your environment.");
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Set OpenAI:ApiKey in appsettings.json or your environment.");

Console.WriteLine($"Model: {model}");
Console.WriteLine($"ImageModel: {imageModel}");
Console.WriteLine($"Endpoint: {endpoint}");

string prompt =
    """
    You are an AI assistant that can give tips about a map. 
    The map is provided as an image as part of the user prompt.
    You need to analyze the image and give information about concerts and venues that happen in this area in the upcoming 3 months.
    Provide information about the concerts in the chat and add location details so I can set pins on this map with an image editing model.
    """;

string imagePath = File.Exists(Path.Combine(AppContext.BaseDirectory, "SanDiego-Area.png"))
    ? Path.Combine(AppContext.BaseDirectory, "SanDiego-Area.png")
    : "SanDiego-Area.png";

byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

var agent = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
                .GetChatClient(model)
                .AsIChatClient()
                .AsAIAgent(instructions: prompt);

// Multimodal messages are built from a list of AIContent parts.
var message = new ChatMessage(ChatRole.User,
[
    new TextContent("Here is the image of the San Diego area:"),
    new DataContent(imageBytes, "image/png")
]);


var result = await agent.RunAsync(message);
Console.WriteLine(result);
//-- No Add the Image Edit. No support yet so Http REST call is used instead.

// Call MAI Image 2.6 Edits REST API
Console.WriteLine("\nSending image and concert info to MAI Image 2.6 model...");

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

var maiUrl = $"{endpoint.TrimEnd('/')}/mai/v1/images/edits";


//Adding new instructions combined with text from previous demo
using var form = new MultipartFormDataContent();
form.Add(new StringContent(imageModel), "model");
form.Add(new StringContent($"Add clear green location pins and labels on this map for the following concert venues and locations:\n{result}"), "prompt");

var imageContent = new ByteArrayContent(imageBytes);
imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
form.Add(imageContent, "image", "SanDiego-Area.png");

using var response = await httpClient.PostAsync(maiUrl, form);
var responseJson = await response.Content.ReadAsStringAsync();

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"Error ({response.StatusCode}): {responseJson}");
    response.EnsureSuccessStatusCode();
}

using var doc = JsonDocument.Parse(responseJson);
var base64Image = doc.RootElement
    .GetProperty("data")[0]
    .GetProperty("b64_json")
    .GetString();


//Write Image
if (!string.IsNullOrEmpty(base64Image))
{
    byte[] outputBytes = Convert.FromBase64String(base64Image);
    string outputPath = Path.Combine(AppContext.BaseDirectory, "SanDiego-Area-Output.png");
    await File.WriteAllBytesAsync(outputPath, outputBytes);
    Console.WriteLine($"\nOutput image successfully saved to: {outputPath}");
}

