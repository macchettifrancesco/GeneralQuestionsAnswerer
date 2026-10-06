using System.Text.Json.Serialization;

namespace TechnicalDocsQa.Api.Infrastructure.Qdrant;

public sealed class QdrantSearchRequest
{
    [JsonPropertyName("vector")]
    public required float[] Vector { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("score_threshold")]
    public float? ScoreThreshold { get; init; }

    [JsonPropertyName("with_payload")]
    public bool WithPayload { get; init; } = true;

    [JsonPropertyName("with_vector")]
    public bool WithVector { get; init; } = false;
}
