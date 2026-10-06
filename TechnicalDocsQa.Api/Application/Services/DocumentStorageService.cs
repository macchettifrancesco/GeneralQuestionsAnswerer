using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class DocumentStorageService : IDocumentStorageService
{
    private readonly string _documentsPath;
    private readonly ILogger<DocumentStorageService> _logger;

    public DocumentStorageService(IConfiguration configuration, ILogger<DocumentStorageService> logger)
    {
        _logger = logger;
        _documentsPath = configuration["Storage:DocumentsPath"] 
            ?? throw new InvalidOperationException("Storage:DocumentsPath configuration is missing.");
        
        // Ensure directory exists
        Directory.CreateDirectory(_documentsPath);
        _logger.LogInformation("DocumentStorageService initialized with path: {DocumentsPath}", _documentsPath);
    }

    public async Task<DocumentRecord> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            throw new InvalidOperationException("Cannot save an empty file.");
        }

        // Generate document ID
        var documentId = Guid.NewGuid();

        // Read file content
        await using var stream = file.OpenReadStream();
        var bytes = new byte[file.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);

        // Compute SHA256 hash
        var contentHash = ComputeSha256Hash(bytes);
        _logger.LogInformation("Computed content hash {ContentHash} for document {DocumentId}.", contentHash, documentId);

        // Sanitize original filename
        var sanitizedOriginalFileName = SanitizeFileName(file.FileName);

        // Generate stored filename and path
        var storedFileName = $"{documentId}.md";
        var storedPath = Path.Combine(_documentsPath, storedFileName);

        // Anti-traversal validation
        ValidatePathSafety(storedPath);

        // Write file to disk
        await File.WriteAllBytesAsync(storedPath, bytes, cancellationToken);
        _logger.LogInformation("Document {DocumentId} saved to {StoredPath}.", documentId, storedPath);

        // Create and return DocumentRecord
        var documentRecord = new DocumentRecord
        {
            DocumentId = documentId,
            OriginalFileName = sanitizedOriginalFileName,
            StoredFileName = storedFileName,
            StoredPath = storedPath,
            ContentHash = contentHash,
            Status = DocumentStatus.Uploaded
        };

        return documentRecord;
    }

    public async Task<string> ReadDocumentContentAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidatePathSafety(document.StoredPath);

        if (!File.Exists(document.StoredPath))
        {
            _logger.LogError("Document file not found for {DocumentId} at path {StoredPath}.", document.DocumentId, document.StoredPath);
            throw new FileNotFoundException($"Document file not found for document {document.DocumentId}.");
        }

        var content = await File.ReadAllTextAsync(document.StoredPath, cancellationToken);
        _logger.LogInformation("Read content for document {DocumentId} ({Length} characters).", document.DocumentId, content.Length);

        return content;
    }

    public async Task DeleteDocumentFileAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidatePathSafety(document.StoredPath);

        if (File.Exists(document.StoredPath))
        {
            await Task.Run(() => File.Delete(document.StoredPath), cancellationToken);
            _logger.LogInformation("Deleted document file for {DocumentId} at {StoredPath}.", document.DocumentId, document.StoredPath);
        }
        else
        {
            _logger.LogWarning("Document file not found for deletion: {DocumentId} at {StoredPath}.", document.DocumentId, document.StoredPath);
        }
    }

    private static string ComputeSha256Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        }

        // Remove path separators and keep only safe characters
        var sanitized = Regex.Replace(fileName, @"[^\w\.\-]", "_");
        
        // Remove any leading/trailing dots or underscores
        sanitized = sanitized.Trim('.', '_');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new ArgumentException("File name contains no valid characters.", nameof(fileName));
        }

        return sanitized;
    }

    private void ValidatePathSafety(string path)
    {
        var resolvedPath = Path.GetFullPath(path);
        var resolvedBasePath = Path.GetFullPath(_documentsPath);

        if (!resolvedPath.StartsWith(resolvedBasePath, StringComparison.Ordinal))
        {
            _logger.LogError("Path traversal attempt detected: {Path}", path);
            throw new InvalidOperationException("Path traversal detected.");
        }
    }
}
