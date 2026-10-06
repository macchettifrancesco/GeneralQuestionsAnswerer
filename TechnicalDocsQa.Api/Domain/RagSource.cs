namespace TechnicalDocsQa.Api.Domain;

public sealed record RagSource(
    Guid DocumentId,
    string SourceFile,
    string SourcePath,
    string? Title,
    string? Section,
    int ChunkIndex,
    float Score);
