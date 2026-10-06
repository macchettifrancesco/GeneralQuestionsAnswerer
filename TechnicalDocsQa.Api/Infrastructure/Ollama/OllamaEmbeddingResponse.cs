using System.Text.Json.Serialization;

namespace TechnicalDocsQa.Api.Infrastructure.Ollama;

public sealed class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = [];
}
