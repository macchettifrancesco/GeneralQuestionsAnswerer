namespace TechnicalDocsQa.Api.Domain;

public sealed record AskQuestionResponse(
    string Answer,
    IReadOnlyList<RagSource> Sources,
    int TokensUsed,
    int SourcesFound,
    DateTime AnsweredAtUtc);
