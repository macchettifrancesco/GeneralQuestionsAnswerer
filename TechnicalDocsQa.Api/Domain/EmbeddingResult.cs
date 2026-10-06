namespace TechnicalDocsQa.Api.Domain;

public sealed class EmbeddingResult
{
    public required string ChunkId { get; init; }
    public required float[] Vector { get; init; }
}
