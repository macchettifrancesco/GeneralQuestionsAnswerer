using System.Security.Cryptography;
using System.Text;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class ChunkingService : IChunkingService
{
    private readonly int _maxChunkCharacters;
    private readonly int _overlapCharacters;
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(IConfiguration configuration, ILogger<ChunkingService> logger)
    {
        _maxChunkCharacters = configuration.GetValue("Chunking:MaxChunkCharacters", 3000);
        _overlapCharacters = configuration.GetValue("Chunking:OverlapCharacters", 400);
        _logger = logger;

        _logger.LogInformation("ChunkingService initialized: MaxChunkCharacters={Max}, OverlapCharacters={Overlap}", 
            _maxChunkCharacters, _overlapCharacters);
    }

    public IReadOnlyList<DocumentChunk> CreateChunks(ParsedMarkdown parsed, Guid documentId, string sourceFile, string sourcePath)
    {
        _logger.LogDebug("Creating chunks for document {DocumentId}", documentId);

        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        if (parsed.Sections.Count > 0)
        {
            // Section-first strategy
            foreach (var section in parsed.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Content))
                {
                    continue; // Skip empty sections
                }

                if (section.Content.Length <= _maxChunkCharacters)
                {
                    // Section fits in one chunk
                    var chunk = CreateChunk(
                        documentId,
                        section.Content.Trim(),
                        sourceFile,
                        sourcePath,
                        section.Heading,
                        section.Heading,
                        chunkIndex++,
                        section.StartIndex,
                        section.StartIndex + section.Content.Length
                    );
                    chunks.Add(chunk);
                }
                else
                {
                    // Section is too long, split with sliding window
                    var sectionChunks = SplitTextWithOverlap(
                        section.Content,
                        documentId,
                        sourceFile,
                        sourcePath,
                        section.Heading,
                        section.Heading,
                        ref chunkIndex,
                        section.StartIndex
                    );
                    chunks.AddRange(sectionChunks);
                }
            }
        }
        else
        {
            // No sections found, split entire text with sliding window
            var textChunks = SplitTextWithOverlap(
                parsed.CleanText,
                documentId,
                sourceFile,
                sourcePath,
                parsed.Title,
                null,
                ref chunkIndex,
                0
            );
            chunks.AddRange(textChunks);
        }

        _logger.LogDebug("Created {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);
        return chunks;
    }

    private List<DocumentChunk> SplitTextWithOverlap(
        string text,
        Guid documentId,
        string sourceFile,
        string sourcePath,
        string title,
        string? section,
        ref int chunkIndex,
        int baseStartIndex)
    {
        var chunks = new List<DocumentChunk>();
        var start = 0;

        while (start < text.Length)
        {
            var end = Math.Min(start + _maxChunkCharacters, text.Length);
            var chunkText = text[start..end].Trim();

            // Skip empty or whitespace-only chunks
            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                var chunk = CreateChunk(
                    documentId,
                    chunkText,
                    sourceFile,
                    sourcePath,
                    title,
                    section,
                    chunkIndex++,
                    baseStartIndex + start,
                    baseStartIndex + end
                );
                chunks.Add(chunk);
            }

            // Move start position with overlap
            if (end >= text.Length)
            {
                break;
            }
            start = end - _overlapCharacters;
        }

        return chunks;
    }

    private static DocumentChunk CreateChunk(
        Guid documentId,
        string text,
        string sourceFile,
        string sourcePath,
        string title,
        string? section,
        int chunkIndex,
        int startCharacter,
        int endCharacter)
    {
        var chunkId = GenerateDeterministicChunkId(documentId, chunkIndex, text);

        return new DocumentChunk
        {
            Id = chunkId,
            DocumentId = documentId,
            Text = text,
            SourceFile = sourceFile,
            SourcePath = sourcePath,
            Title = title,
            Section = section,
            ChunkIndex = chunkIndex,
            StartCharacter = startCharacter,
            EndCharacter = endCharacter,
            IndexedAtUtc = DateTime.UtcNow
        };
    }

    private static string GenerateDeterministicChunkId(Guid documentId, int chunkIndex, string text)
    {
        var input = $"{documentId}:{chunkIndex}:{text}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = hashBytes[..16];
        return new Guid(guidBytes).ToString();
    }
}
