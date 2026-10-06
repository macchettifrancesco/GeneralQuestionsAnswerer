namespace TechnicalDocsQa.Blazor.Models;

public sealed record AskQuestionRequest(string Question);

public sealed record AskQuestionResponse(
    string Answer,
    List<RagSource> Sources,
    int TokensUsed,
    int SourcesFound,
    DateTime AnsweredAtUtc);

public sealed record RagSource(
    Guid DocumentId,
    string SourceFile,
    string SourcePath,
    string? Title,
    string? Section,
    int ChunkIndex,
    float Score);
