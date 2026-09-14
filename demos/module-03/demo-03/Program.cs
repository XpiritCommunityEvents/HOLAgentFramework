using CommunityToolkit.VectorData.InMemory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using modulerag;
using System.ClientModel;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
var endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("OpenAI:EndPoint is not configured.");
var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

var client = new OpenAI.Embeddings.EmbeddingClient(
    "text-embedding-3-small",
    new ApiKeyCredential(token),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
    });

VectorStore vectorStore = new InMemoryVectorStore();
var collection = vectorStore.GetCollection<ulong, PolicyFilePart>("venue-policies");
await collection.EnsureCollectionExistsAsync();

var embeddingGenerator = client.AsIEmbeddingGenerator(defaultModelDimensions: 1536);

var chat = new ChatWithRag();
await chat.IngestDocuments(collection, embeddingGenerator);

var ragContextProvider = new RagContextProvider(collection, embeddingGenerator);

AIAgent agent = new OpenAI.Chat.ChatClient(
    model,
    new ApiKeyCredential(token),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
    })
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "Assistant",
        //here we inject the context provider for RAG (Retrieval-Augmented Generation)
        AIContextProviders = [ragContextProvider],
        ChatOptions = new ChatOptions
        {
            Instructions = """
                You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
                Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that 
                you don't know the answer based on your knowledge.
                Always use the venue policy information provided in your context.
                """
        }
    });

await chat.RAG_with_memory(agent);

