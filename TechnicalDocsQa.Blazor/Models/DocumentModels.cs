namespace TechnicalDocsQa.Blazor.Models;

public sealed record DocumentRecord(
    Guid DocumentId,
    string OriginalFileName,
    string StoredFileName,
    string StoredPath,
    string ContentHash,
    int Status,
    int? ChunksCount,
    string? ErrorMessage,
    DateTime UploadedAtUtc,
    DateTime? IndexedAtUtc,
    DateTime? DeletedAtUtc);

public enum DocumentStatus
{
    Uploaded,
    Indexing,
    Indexed,
    Failed,
    Deleted
}

public sealed record UploadDocumentResponse(
    Guid DocumentId,
    string FileName,
    string Status,
    int? ChunksCreated,
    string Message);
