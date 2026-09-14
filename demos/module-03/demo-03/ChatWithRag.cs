using MarkdownStructureChunker.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

using System.Text.Json;

namespace modulerag;

public class ChatWithRag
{
    public async Task RAG_with_memory(AIAgent agent)
    {
        var question =
            """
            I booked tickets for a concert tonight in venue AFAS Live!.
            I have this small black backpack, not big like for school, more like the mini
            festival type 😅. it just fits my wallet, a hoodie and a bottle of water.
            Is this allowed? 
            """;

        var response = await GetResponseOnQuestion(agent, question);

        Console.WriteLine("******** RESPONSE WITH MEMORY ***********");
        Console.WriteLine(response);
    }



    public async Task IngestDocuments(VectorStoreCollection<ulong, PolicyFilePart> collection, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        var chunker = StructureChunker.CreateStructureFirst();

        List<PolicyFilePart> files = [];

        ulong key = 0;
        var directory = "../../../../datasets/venue-policies";
        foreach (var file in GetFileListOfPolicyDocuments(directory))
        {
            var fullfilename = Path.Combine(directory, file);

            // mimic a persistent storage using json serializations
            // you would normally use a database for this
            var jsonCacheFileName = Path.ChangeExtension(fullfilename, ".json");
            if (File.Exists(jsonCacheFileName))
            {
                var cachedChunks = JsonSerializer.Deserialize<PolicyFilePart[]>(File.ReadAllBytes(jsonCacheFileName));
                files.AddRange(cachedChunks!);
                Console.WriteLine($"Loaded cached venue policy file from {jsonCacheFileName}");
            }
            else
            {
                List<PolicyFilePart> venueChunks = [];

                // Chunk the MD file by its structure (headings, numbered lists, etc)
                string fileContent = File.ReadAllText(fullfilename);
                var chunks = await chunker.ChunkAsync(fileContent);

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

                using var jsonFile = File.OpenWrite(jsonCacheFileName);
                await JsonSerializer.SerializeAsync(jsonFile, venueChunks);

                Console.WriteLine($"Imported file {file} with {chunks.Count()} chunks");

                files.AddRange(venueChunks);
            }
        }

        await collection.UpsertAsync(files);
    }

    private async Task<string> GetResponseOnQuestion(AIAgent agent, string question)
    {
        // Retrieval is handled by RagContextProvider, which is wired into the agent's options.
        AgentSession session = await agent.CreateSessionAsync();
        var result = await agent.RunAsync(question, session);
        return result.Text;
    }

   

    private IEnumerable<string> GetFileListOfPolicyDocuments(string directory)
    {
        return Directory.GetFiles(directory, "*.md").Select(f => Path.GetFileName(f));
    }
}