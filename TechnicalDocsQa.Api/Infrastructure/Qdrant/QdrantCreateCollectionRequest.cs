using System.Text.Json.Serialization;

namespace TechnicalDocsQa.Api.Infrastructure.Qdrant;

public sealed class QdrantCreateCollectionRequest
{
    [JsonPropertyName("vectors")]
    public required QdrantVectorParams Vectors { get; init; }
}

public sealed class QdrantVectorParams
{
    [JsonPropertyName("size")]
    public required int Size { get; init; }

    [JsonPropertyName("distance")]
    public required string Distance { get; init; }
}
