namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IIndexingQueue
{
    ValueTask QueueDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default);
}
