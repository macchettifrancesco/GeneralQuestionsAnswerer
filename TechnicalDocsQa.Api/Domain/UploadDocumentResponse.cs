namespace TechnicalDocsQa.Api.Domain;

public sealed class UploadDocumentResponse
{
    public required Guid DocumentId { get; init; }
    public required string FileName { get; init; }
    public required string Status { get; init; }
    public int? ChunksCreated { get; init; }
    public required string Message { get; init; }
}
