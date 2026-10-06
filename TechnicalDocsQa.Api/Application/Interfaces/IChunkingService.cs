using TechnicalDocsQa.Api.Application.Services;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> CreateChunks(ParsedMarkdown parsed, Guid documentId, string sourceFile, string sourcePath);
}
