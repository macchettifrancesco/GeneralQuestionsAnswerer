using TechnicalDocsQa.Api.Application.Services;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IMarkdownParserService
{
    ParsedMarkdown Parse(string filePath, string sourceFile, string sourcePath);
}
