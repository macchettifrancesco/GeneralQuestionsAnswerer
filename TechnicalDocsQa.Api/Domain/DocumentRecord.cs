namespace TechnicalDocsQa.Api.Domain;

public sealed class DocumentRecord
{
    public required Guid DocumentId { get; init; }
    public required string OriginalFileName { get; init; }
    public required string StoredFileName { get; init; }
    public required string StoredPath { get; init; }
    public required string ContentHash { get; init; }
    public required DocumentStatus Status { get; set; }
    public int ChunksCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? IndexedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
