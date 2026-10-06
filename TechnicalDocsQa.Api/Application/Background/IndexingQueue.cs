using System.Threading.Channels;
using TechnicalDocsQa.Api.Application.Interfaces;

namespace TechnicalDocsQa.Api.Application.Background;

public sealed class IndexingQueue : IIndexingQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public async ValueTask QueueDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(documentId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
