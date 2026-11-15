using Microsoft.Extensions.VectorData;

namespace modulerag;

/// <summary>
/// Class representing a chunk of text from a venue policy file, along with its embedding vector for use in a vector store.
/// Every instance of this class becomes an entry in the a vector store collection
/// </summary>
public sealed class PolicyFilePart
{
    public ulong Key { get; set; }

    public required string FileName { get; init; }

    public required string Chunk { get; init; }

    public ReadOnlyMemory<float> EmbeddingVector { get; set; }

    // The schema is defined at runtime because the vector size depends on the configured embedding model.
    public static VectorStoreCollectionDefinition CreateDefinition(int dimensions) => new()
    {
        Properties =
        [
            new VectorStoreKeyProperty(nameof(Key), typeof(ulong)),
            new VectorStoreDataProperty(nameof(FileName), typeof(string)) { IsIndexed = true },
            new VectorStoreDataProperty(nameof(Chunk), typeof(string)),
            new VectorStoreVectorProperty(nameof(EmbeddingVector), typeof(ReadOnlyMemory<float>), dimensions),
        ]
    };
}
