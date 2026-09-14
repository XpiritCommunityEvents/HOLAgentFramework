using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace modulerag;

/// <summary>
/// Context provider that performs retrieval-augmented generation: it embeds the latest user
/// question, searches the venue policy vector store, and injects the matching chunks as
/// instructions for the agent's next turn.
/// </summary>
internal sealed class RagContextProvider(
    VectorStoreCollection<ulong, PolicyFilePart> collection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    int topK = 5) : AIContextProvider
{
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var question = context.AIContext.Messages?.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        if (string.IsNullOrWhiteSpace(question))
        {
            return new AIContext();
        }

        var questionEmbedding = await embeddingGenerator.GenerateAsync(question, cancellationToken: cancellationToken);
        var searchResults = await collection.SearchAsync(questionEmbedding.Vector, top: topK, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var policyContext = searchResults.Count > 0
            ? string.Join("\n\n", searchResults.Select(r => r.Record.Chunk))
            : "No information found";
        Console.WriteLine("---------------");
        Console.WriteLine("Providing RAG context with the following policy information:");
        Console.WriteLine(policyContext);
        Console.WriteLine("---------------");
        return new AIContext
        {

            Instructions = $"""
                Always use the following venue policy information to answer the user's question.
                ### Venue Policy
                {policyContext}
                """
        };
    }
}
