using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class DocumentIndexingService : IDocumentIndexingService
{
    private readonly IDocumentRegistryService _registry;
    private readonly IDocumentStorageService _storage;
    private readonly IMarkdownParserService _markdownParser;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger<DocumentIndexingService> _logger;

    public DocumentIndexingService(
        IDocumentRegistryService registry,
        IDocumentStorageService storage,
        IMarkdownParserService markdownParser,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStore,
        ILogger<DocumentIndexingService> logger)
    {
        _registry = registry;
        _storage = storage;
        _markdownParser = markdownParser;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<int> IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Indexing started for document {DocumentId}.", documentId);

        try
        {
            // 1. Get document record
            var document = await _registry.GetByIdAsync(documentId, cancellationToken);
            if (document is null)
            {
                throw new InvalidOperationException($"Document {documentId} not found in registry.");
            }

            // 2. Set status to Indexing
            document.Status = DocumentStatus.Indexing;
            document.ErrorMessage = null;
            await _registry.UpdateAsync(document, cancellationToken);

            // 3. Ensure Qdrant collection exists
            await _vectorStore.EnsureCollectionExistsAsync(cancellationToken);

            // 4. Parse document (supports .md, .txt, .pdf)
            var parsed = _markdownParser.Parse(document.StoredPath, document.OriginalFileName, document.StoredPath);
            _logger.LogInformation("Parsed {SectionCount} sections from document {DocumentId}.", 
                parsed.Sections.Count, documentId);

            // 6. Create chunks
            var chunks = _chunkingService.CreateChunks(parsed, documentId, document.OriginalFileName, document.StoredPath);
            _logger.LogInformation("Created {ChunkCount} chunks for document {DocumentId}.", 
                chunks.Count, documentId);

            // 7. Delete existing chunks from vector store
            await _vectorStore.DeleteByDocumentIdAsync(documentId, cancellationToken);

            // 8. Generate embeddings for all chunks
            _logger.LogInformation("Generating embeddings for {ChunkCount} chunks...", chunks.Count);
            var embeddings = new Dictionary<string, float[]>();
            
            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text, cancellationToken);
                embeddings[chunk.Id] = embedding;
            }

            // 9. Upsert chunks with embeddings to vector store
            await _vectorStore.UpsertChunksAsync(chunks, embeddings, cancellationToken);

            // 10. Update document status to Indexed
            document.Status = DocumentStatus.Indexed;
            document.ChunksCount = chunks.Count;
            document.IndexedAtUtc = DateTime.UtcNow;
            await _registry.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Indexing completed for document {DocumentId}. Chunks: {ChunkCount}.", 
                documentId, chunks.Count);

            return chunks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed for document {DocumentId}.", documentId);

            // Update document status to Failed
            try
            {
                var document = await _registry.GetByIdAsync(documentId, cancellationToken);
                if (document is not null)
                {
                    document.Status = DocumentStatus.Failed;
                    document.ErrorMessage = ex.Message;
                    await _registry.UpdateAsync(document, cancellationToken);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update document {DocumentId} status to Failed.", documentId);
            }

            throw;
        }
    }
}
