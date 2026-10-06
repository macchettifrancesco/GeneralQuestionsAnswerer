using TechnicalDocsQa.Api.Application.Interfaces;

namespace TechnicalDocsQa.Api.Application.Background;

public sealed class DocumentIndexingBackgroundService : BackgroundService
{
    private readonly IIndexingQueue _queue;
    private readonly IDocumentIndexingService _indexingService;
    private readonly ILogger<DocumentIndexingBackgroundService> _logger;

    public DocumentIndexingBackgroundService(
        IIndexingQueue queue,
        IDocumentIndexingService indexingService,
        ILogger<DocumentIndexingBackgroundService> logger)
    {
        _queue = queue;
        _indexingService = indexingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentIndexingBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Dequeue the next document ID
                var documentId = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Processing indexing request for document {DocumentId}.", documentId);

                // Index the document directly (all services are Singleton)
                await _indexingService.IndexDocumentAsync(documentId, stoppingToken);

                _logger.LogInformation("Successfully processed indexing request for document {DocumentId}.", documentId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                // Log the error but keep the loop alive
                _logger.LogError(ex, "Error processing indexing request.");
            }
        }

        _logger.LogInformation("DocumentIndexingBackgroundService stopped.");
    }
}
