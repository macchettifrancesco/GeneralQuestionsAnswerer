using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Controllers;

[Route("documents")]
[ApiController]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentStorageService _storageService;
    private readonly IDocumentRegistryService _registryService;
    private readonly IIndexingQueue _indexingQueue;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentStorageService storageService,
        IDocumentRegistryService registryService,
        IIndexingQueue indexingQueue,
        IVectorStoreService vectorStoreService,
        IConfiguration configuration,
        ILogger<DocumentsController> logger)
    {
        _storageService = storageService;
        _registryService = registryService;
        _indexingQueue = indexingQueue;
        _vectorStoreService = vectorStoreService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new Markdown document for indexing.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)] // 50 MB hard cap
    public async Task<ActionResult<UploadDocumentResponse>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        // Validation: file null or empty
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload rejected: No file provided or file is empty.");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file",
                Detail = "No file provided or file is empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation("Upload received: {FileName} ({Size} bytes).", file.FileName, file.Length);

        // Validation: extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".md")
        {
            _logger.LogWarning("Upload rejected: Invalid extension {Extension}.", extension);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file type",
                Detail = "Only .md files are accepted.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Validation: file size
        var maxFileSizeMb = _configuration.GetValue<int>("Storage:MaxFileSizeMb", 10);
        var maxFileSizeBytes = maxFileSizeMb * 1024 * 1024;
        if (file.Length > maxFileSizeBytes)
        {
            _logger.LogWarning("Upload rejected: File size {Size} exceeds limit {Limit} bytes.", file.Length, maxFileSizeBytes);
            return BadRequest(new ProblemDetails
            {
                Title = "File too large",
                Detail = $"File exceeds maximum allowed size of {maxFileSizeMb} MB.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            // Read file to bytes for hash computation
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.OpenReadStream().CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }

            // Compute SHA256 hash for duplicate detection
            string contentHash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(fileBytes);
                contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }

            // Duplicate detection
            var existingDocument = await _registryService.FindByHashAsync(contentHash, cancellationToken);
            if (existingDocument != null && existingDocument.Status != DocumentStatus.Deleted)
            {
                _logger.LogInformation("Document with hash {Hash} already exists as {DocumentId}.", contentHash, existingDocument.DocumentId);
                return Ok(new UploadDocumentResponse
                {
                    DocumentId = existingDocument.DocumentId,
                    FileName = existingDocument.OriginalFileName,
                    Status = existingDocument.Status.ToString(),
                    ChunksCreated = existingDocument.ChunksCount > 0 ? existingDocument.ChunksCount : null,
                    Message = "Document already exists and is already indexed."
                });
            }

            // Happy path: save, register, and queue
            var record = await _storageService.SaveUploadedFileAsync(file, cancellationToken);
            await _registryService.AddAsync(record, cancellationToken);
            await _indexingQueue.QueueDocumentAsync(record.DocumentId, cancellationToken);

            _logger.LogInformation("Document {DocumentId} queued for indexing.", record.DocumentId);

            return Accepted(new UploadDocumentResponse
            {
                DocumentId = record.DocumentId,
                FileName = record.OriginalFileName,
                Status = "Uploaded",
                ChunksCreated = null,
                Message = "Document uploaded. Indexing has been queued."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during upload.");
            return Problem(
                title: "Upload failed",
                detail: "An unexpected error occurred while uploading the document.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get all documents in the registry.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentRecord>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var documents = await _registryService.GetAllAsync(cancellationToken);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all documents.");
            return Problem(
                title: "Retrieval failed",
                detail: "An error occurred while retrieving documents.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get a specific document by ID.
    /// </summary>
    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<DocumentRecord>> GetById(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _registryService.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found.", documentId);
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Document with ID {documentId} does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}.", documentId);
            return Problem(
                title: "Retrieval failed",
                detail: "An error occurred while retrieving the document.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Delete a document and its associated vector store chunks.
    /// </summary>
    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            // Load document
            var document = await _registryService.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Delete failed: Document {DocumentId} not found.", documentId);
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Document with ID {documentId} does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Delete physical file
            await _storageService.DeleteDocumentFileAsync(document, cancellationToken);

            // Delete from vector store
            await _vectorStoreService.DeleteByDocumentIdAsync(documentId, cancellationToken);

            // Update registry status
            document.Status = DocumentStatus.Deleted;
            document.DeletedAtUtc = DateTime.UtcNow;
            await _registryService.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Document {DocumentId} deleted successfully.", documentId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}.", documentId);
            return Problem(
                title: "Deletion failed",
                detail: "An error occurred while deleting the document.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Reindex an existing document.
    /// </summary>
    [HttpPost("{documentId:guid}/reindex")]
    public async Task<IActionResult> Reindex(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            // Load document
            var document = await _registryService.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
            {
                _logger.LogWarning("Reindex failed: Document {DocumentId} not found.", documentId);
                return NotFound(new ProblemDetails
                {
                    Title = "Document not found",
                    Detail = $"Document with ID {documentId} does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Check if deleted
            if (document.Status == DocumentStatus.Deleted)
            {
                _logger.LogWarning("Reindex rejected: Document {DocumentId} is deleted.", documentId);
                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot reindex deleted document",
                    Detail = "Cannot reindex a deleted document.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Reset status and queue for indexing
            document.Status = DocumentStatus.Uploaded;
            document.ErrorMessage = null;
            await _registryService.UpdateAsync(document, cancellationToken);
            await _indexingQueue.QueueDocumentAsync(documentId, cancellationToken);

            _logger.LogInformation("Document {DocumentId} queued for reindexing.", documentId);

            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing document {DocumentId}.", documentId);
            return Problem(
                title: "Reindex failed",
                detail: "An error occurred while reindexing the document.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
