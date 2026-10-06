using System.Text.Json.Serialization;

namespace TechnicalDocsQa.Api.Infrastructure.Qdrant;

public sealed class QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public List<QdrantSearchResult> Result { get; init; } = new();
}

public sealed class QdrantSearchResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("score")]
    public float Score { get; init; }

    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; init; } = new();
}
