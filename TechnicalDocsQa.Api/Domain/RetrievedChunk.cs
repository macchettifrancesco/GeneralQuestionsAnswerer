namespace TechnicalDocsQa.Api.Domain;

public sealed record RetrievedChunk(
    string Id,
    Guid DocumentId,
    string Text,
    string SourceFile,
    string SourcePath,
    string? Title,
    string? Section,
    int ChunkIndex,
    DateTime IndexedAtUtc,
    float Score);
