using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IDocumentRegistryService
{
    Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default);
    
    Task<DocumentRecord?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<DocumentRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<DocumentRecord?> FindByHashAsync(string contentHash, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(DocumentRecord document, CancellationToken cancellationToken = default);
}
