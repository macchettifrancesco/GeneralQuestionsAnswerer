namespace TechnicalDocsQa.Api.Application.Services;

public sealed class MarkdownSection
{
    public required string Heading { get; init; }
    public int Level { get; init; }
    public required string Content { get; init; }
    public int StartIndex { get; init; }
}
