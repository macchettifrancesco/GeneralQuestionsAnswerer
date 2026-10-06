using System.Text.Json.Serialization;

namespace TechnicalDocsQa.Api.Infrastructure.Qdrant;

public sealed class QdrantPoint
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("vector")]
    public required float[] Vector { get; init; }

    [JsonPropertyName("payload")]
    public required Dictionary<string, object> Payload { get; init; }
}
