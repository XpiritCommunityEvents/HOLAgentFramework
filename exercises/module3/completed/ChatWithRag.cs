using MarkdownStructureChunker.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Text.Json;

namespace modulerag;

public class ChatWithRag
{
    public async Task RAG_with_memory(
        AIAgent agent,
        VectorStoreCollection<ulong, PolicyFilePart> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        var question =
            """
            I booked tickets for a concert tonight in venue AFAS Live!.
            I have this small black backpack, not big like for school, more like the mini
            festival type 😅. it just fits my wallet, a hoodie and a bottle of water.
            Is this allowed?
            """;

        var questionEmbedding = await embeddingGenerator.GenerateAsync(question);
        var searchResult = await collection.SearchAsync(questionEmbedding.Vector, top: 1).ToListAsync();
        var policyContext = searchResult.FirstOrDefault()?.Record?.Chunk ?? "No information found";
        var response = await GetResponseOnQuestion(agent, question, policyContext);

        Console.WriteLine("******** RESPONSE WITH MEMORY ***********");
        Console.WriteLine(response);
    }

    public async Task AskVenueQuestion(
        AIAgent agent,
        VectorStoreCollection<ulong, PolicyFilePart> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        const string question = "Which venue allows a backpack?";

        var questionEmbedding = await embeddingGenerator.GenerateAsync(question);
        var searchResult = await collection.SearchAsync(questionEmbedding.Vector, top: 50).ToListAsync();
        var policyContext = string.Join("\n\n", searchResult.Select(result => result.Record.Chunk));
        var response = await GetResponseOnQuestion(agent, question, policyContext);

        Console.WriteLine("******** RESPONSE WITH MEMORY ***********");
        Console.WriteLine(response);
    }

    public async Task IngestDocuments(
        VectorStoreCollection<ulong, PolicyFilePart> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        var chunker = StructureChunker.CreateStructureFirst();
        List<PolicyFilePart> files = [];
        ulong key = 0;
        var directory = "../../../../datasets/venue-policies";

        foreach (var file in GetFileListOfPolicyDocuments(directory))
        {
            var fullFileName = Path.Combine(directory, file);

            // Mimic persistent storage using JSON serialization. A real application
            // would normally use a database-backed vector store.
            var jsonCacheFileName = Path.ChangeExtension(fullFileName, ".json");
            if (File.Exists(jsonCacheFileName))
            {
                var cachedChunks = JsonSerializer.Deserialize<PolicyFilePart[]>(File.ReadAllBytes(jsonCacheFileName));
                files.AddRange(cachedChunks ?? []);
                Console.WriteLine($"Loaded cached venue policy file from {jsonCacheFileName}");
                continue;
            }

            List<PolicyFilePart> venueChunks = [];
            var fileContent = File.ReadAllText(fullFileName);
            var chunks = (await chunker.ChunkAsync(fileContent)).ToList();

            foreach (var chunk in chunks)
            {
                var filePart = new PolicyFilePart
                {
                    Key = key++,
                    FileName = file,
                    Chunk = $"# Venue: {file}\n{chunk.Content}",
                };

                var embedding = await embeddingGenerator.GenerateAsync(filePart.Chunk);
                filePart.EmbeddingVector = embedding.Vector;
                venueChunks.Add(filePart);
            }

            await using var jsonFile = File.Create(jsonCacheFileName);
            await JsonSerializer.SerializeAsync(jsonFile, venueChunks);

            Console.WriteLine($"Imported file {file} with {chunks.Count} chunks");
            files.AddRange(venueChunks);
        }

        await collection.UpsertAsync(files);
    }

    private static async Task<string> GetResponseOnQuestion(AIAgent agent, string question, string policyContext)
    {
        var systemMessage = $"""
            You are a helpful assistant that answers questions from people that go to a concert and have questions about the venue.
            Always use the policy information provided in the prompt.
            ### Venue Policy
            {policyContext}
            """;

        var session = await agent.CreateSessionAsync();
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.System, systemMessage),
            new ChatMessage(ChatRole.User, question)
        ]);

        var result = await agent.RunAsync(session);
        return result.Text;
    }

    private static IEnumerable<string> GetFileListOfPolicyDocuments(string directory) =>
        Directory.GetFiles(directory, "*.md").Select(Path.GetFileName).OfType<string>();
}
