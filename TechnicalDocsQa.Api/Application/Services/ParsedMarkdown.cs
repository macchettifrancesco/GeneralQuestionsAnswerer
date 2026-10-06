namespace TechnicalDocsQa.Api.Application.Services;

public sealed class ParsedMarkdown
{
    public required string Title { get; init; }
    public required string CleanText { get; init; }
    public required IReadOnlyList<MarkdownSection> Sections { get; init; }
}
