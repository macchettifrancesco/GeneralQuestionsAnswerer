using Microsoft.AspNetCore.Http;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<DocumentRecord> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    
    Task<string> ReadDocumentContentAsync(DocumentRecord document, CancellationToken cancellationToken = default);
    
    Task DeleteDocumentFileAsync(DocumentRecord document, CancellationToken cancellationToken = default);
}
