using System.Text.Json;
using System.Text.Json.Serialization;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class DocumentRegistryService : IDocumentRegistryService
{
    private readonly string _registryPath;
    private readonly ILogger<DocumentRegistryService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentRegistryService(IConfiguration configuration, ILogger<DocumentRegistryService> logger)
    {
        _logger = logger;
        _registryPath = configuration["Storage:RegistryPath"] 
            ?? throw new InvalidOperationException("Storage:RegistryPath configuration is missing.");
        
        // Ensure directory exists
        var registryDirectory = Path.GetDirectoryName(_registryPath);
        if (!string.IsNullOrEmpty(registryDirectory))
        {
            Directory.CreateDirectory(registryDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        _logger.LogInformation("DocumentRegistryService initialized with path: {RegistryPath}", _registryPath);
    }

    public async Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var documents = await ReadAllDocumentsAsync(cancellationToken);

            // Check for duplicate document ID
            if (documents.Any(d => d.DocumentId == document.DocumentId))
            {
                throw new InvalidOperationException($"Document with ID {document.DocumentId} already exists in registry.");
            }

            documents.Add(document);
            await WriteAllDocumentsAsync(documents, cancellationToken);

            _logger.LogInformation("Added document {DocumentId} to registry. Status: {Status}.", document.DocumentId, document.Status);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DocumentRecord?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var documents = await ReadAllDocumentsAsync(cancellationToken);
            var document = documents.FirstOrDefault(d => d.DocumentId == documentId);

            if (document != null)
            {
                _logger.LogInformation("Retrieved document {DocumentId} from registry.", documentId);
            }
            else
            {
                _logger.LogInformation("Document {DocumentId} not found in registry.", documentId);
            }

            return document;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<DocumentRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var documents = await ReadAllDocumentsAsync(cancellationToken);
            // Filter out deleted documents
            var activeDocuments = documents.Where(d => d.Status != DocumentStatus.Deleted).ToList();
            _logger.LogInformation("Retrieved {Count} active documents from registry (excluding deleted).", activeDocuments.Count);
            return activeDocuments.AsReadOnly();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DocumentRecord?> FindByHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contentHash);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var documents = await ReadAllDocumentsAsync(cancellationToken);
            var document = documents.FirstOrDefault(d => 
                d.ContentHash.Equals(contentHash, StringComparison.OrdinalIgnoreCase) 
                && d.Status != DocumentStatus.Deleted);

            if (document != null)
            {
                _logger.LogInformation("Found document {DocumentId} with hash {ContentHash}.", document.DocumentId, contentHash);
            }
            else
            {
                _logger.LogInformation("No non-deleted document found with hash {ContentHash}.", contentHash);
            }

            return document;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var documents = await ReadAllDocumentsAsync(cancellationToken);
            var index = documents.FindIndex(d => d.DocumentId == document.DocumentId);

            if (index == -1)
            {
                throw new InvalidOperationException($"Document with ID {document.DocumentId} not found in registry.");
            }

            documents[index] = document;
            await WriteAllDocumentsAsync(documents, cancellationToken);

            _logger.LogInformation("Registry updated for document {DocumentId}. New status: {Status}.", document.DocumentId, document.Status);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<List<DocumentRecord>> ReadAllDocumentsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_registryPath))
        {
            _logger.LogWarning("Registry file not found at {RegistryPath}. Returning empty list.", _registryPath);
            return new List<DocumentRecord>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_registryPath, cancellationToken);
            var documents = JsonSerializer.Deserialize<List<DocumentRecord>>(json, _jsonOptions);
            return documents ?? new List<DocumentRecord>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize registry file at {RegistryPath}. Returning empty list.", _registryPath);
            return new List<DocumentRecord>();
        }
    }

    private async Task WriteAllDocumentsAsync(List<DocumentRecord> documents, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(documents, _jsonOptions);
        await File.WriteAllTextAsync(_registryPath, json, cancellationToken);
        _logger.LogInformation("Registry file written to {RegistryPath} with {Count} documents.", _registryPath, documents.Count);
    }
}
