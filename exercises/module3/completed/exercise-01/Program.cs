using CommunityToolkit.VectorData.InMemory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using modulerag;
using System.ClientModel;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
var embeddingModel = config["OpenAI:EmbeddingModel"] ?? throw new InvalidOperationException("OpenAI:EmbeddingModel is not configured.");
var endpoint = config["OpenAI:Endpoint"] ?? throw new InvalidOperationException("OpenAI:Endpoint is not configured.");
var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

AIAgent agent = new OpenAI.OpenAIClient(
    new ApiKeyCredential(token),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
    })
    .GetResponsesClient()
    .AsAIAgent(model: model,
        instructions: """
            You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
            Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that
            you don't know the answer based on your knowledge.
        """,
        name: "Assistant");

var embeddingClient = new OpenAI.Embeddings.EmbeddingClient(
    embeddingModel,
    new ApiKeyCredential(token),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
    });

VectorStore vectorStore = new InMemoryVectorStore();
var collection = vectorStore.GetCollection<ulong, PolicyFilePart>("venue-policies");
await collection.EnsureCollectionExistsAsync();

var embeddingGenerator = embeddingClient.AsIEmbeddingGenerator(defaultModelDimensions: 1536);
var chat = new ChatWithRag();

await chat.IngestDocuments(collection, embeddingGenerator);
await chat.RAG_with_memory(agent, collection, embeddingGenerator);
// await chat.AskVenueQuestion(agent, collection, embeddingGenerator);
