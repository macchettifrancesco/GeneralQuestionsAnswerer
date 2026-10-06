using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IVectorStoreService
{
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
    
    Task UpsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, IReadOnlyDictionary<string, float[]> embeddings, CancellationToken cancellationToken = default);
    
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<RetrievedChunk>> SearchSimilarChunksAsync(
        float[] queryVector, 
        int topK, 
        float? minScore = null, 
        CancellationToken cancellationToken = default);
}
