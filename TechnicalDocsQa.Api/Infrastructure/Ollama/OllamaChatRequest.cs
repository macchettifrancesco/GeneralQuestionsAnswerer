using System.Text.Json.Serialization;

namespace TechnicalDocsQa.Api.Infrastructure.Ollama;

public sealed class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required List<OllamaChatMessage> Messages { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    [JsonPropertyName("options")]
    public OllamaChatOptions? Options { get; init; }
}

public sealed class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public sealed class OllamaChatOptions
{
    [JsonPropertyName("temperature")]
    public float? Temperature { get; init; }

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; init; }
}
