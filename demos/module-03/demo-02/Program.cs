using CommunityToolkit.VectorData.InMemory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using modulerag;
using System.ClientModel;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure proper encoding for console output, especially for special characters.

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
var endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("OpenAI:EndPoint is not configured.");
var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

AIAgent agent = new OpenAI.OpenAIClient(
    new ApiKeyCredential(token),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
    })
    .GetResponsesClient()
    .AsAIAgent(model: model, instructions: """
        You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
        Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that 
        you don't know the answer based on your knowledge.
    """,
                name: "Assistant");

var embeddingProvider = config["Embeddings:Provider"] ?? throw new InvalidOperationException("Embeddings:Provider is not configured.");
var embeddingConfig = config.GetSection($"Embeddings:{embeddingProvider}");
var embeddingModel = embeddingConfig["Model"] ?? throw new InvalidOperationException($"Embeddings:{embeddingProvider}:Model is not configured.");
var embeddingDimensions = int.TryParse(embeddingConfig["Dimensions"], out var dimensions)
    ? dimensions
    : throw new InvalidOperationException($"Embeddings:{embeddingProvider}:Dimensions is not configured.");

// Both clients expose the same IEmbeddingGenerator abstraction, so the rest of the sample is provider-agnostic.
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = embeddingProvider switch
{
    "Ollama" => new OllamaSharp.OllamaApiClient(
        embeddingConfig["Endpoint"] ?? throw new InvalidOperationException("Embeddings:Ollama:Endpoint is not configured."),
        embeddingModel),
    "OpenAI" => new OpenAI.Embeddings.EmbeddingClient(
        embeddingModel,
        new ApiKeyCredential(token),
        new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
        })
        .AsIEmbeddingGenerator(defaultModelDimensions: embeddingDimensions),
    _ => throw new InvalidOperationException($"Unsupported Embeddings:Provider '{embeddingProvider}'. Use 'Ollama' or 'OpenAI'.")
};

VectorStore vectorStore = new InMemoryVectorStore();
var collection = vectorStore.GetCollection<ulong, PolicyFilePart>("venue-policies", PolicyFilePart.CreateDefinition(embeddingDimensions));
await collection.EnsureCollectionExistsAsync();

var chat = new ChatWithRag(
    cacheName: $"{embeddingProvider}-{embeddingModel}".ToLowerInvariant(),
    queryPrompt: embeddingConfig["QueryPrompt"] ?? "",
    documentPrompt: embeddingConfig["DocumentPrompt"] ?? "");

await chat.IngestDocuments(collection, embeddingGenerator);
await chat.RAG_with_memory(agent, collection, embeddingGenerator);
//await chat.AskVenueQuestion(agent, collection, embeddingGenerator);
