namespace TechnicalDocsQa.Api.Domain;

public sealed class DocumentChunk
{
    public required string Id { get; init; }
    public required Guid DocumentId { get; init; }
    public required string Text { get; init; }
    public required string SourceFile { get; init; }
    public required string SourcePath { get; init; }
    public string? Title { get; init; }
    public string? Section { get; init; }
    public int ChunkIndex { get; init; }
    public int StartCharacter { get; init; }
    public int EndCharacter { get; init; }
    public DateTime IndexedAtUtc { get; init; } = DateTime.UtcNow;
}
