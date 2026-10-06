namespace TechnicalDocsQa.Api.Domain;

public sealed record ChatCompletionResult(
    string Answer,
    int TokensUsed);
