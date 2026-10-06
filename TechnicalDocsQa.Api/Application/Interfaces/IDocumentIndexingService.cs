namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IDocumentIndexingService
{
    Task<int> IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
