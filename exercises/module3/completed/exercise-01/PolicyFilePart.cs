using Microsoft.Extensions.VectorData;

namespace modulerag;

/// <summary>
/// A chunk of a venue policy and its embedding vector, stored as one vector-store record.
/// </summary>
public sealed class PolicyFilePart
{
    [VectorStoreKey]
    public ulong Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string FileName { get; init; }

    [VectorStoreData]
    public required string Chunk { get; init; }

    [VectorStoreVector(1536)]
    public ReadOnlyMemory<float> EmbeddingVector { get; set; }
}
