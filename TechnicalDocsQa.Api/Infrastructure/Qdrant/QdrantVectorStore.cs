using System.Net;
using System.Net.Http.Json;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Infrastructure.Qdrant;

public sealed class QdrantVectorStore : IVectorStoreService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly string _collectionName;
    private readonly int _vectorSize;
    private readonly string _distance;

    public QdrantVectorStore(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantVectorStore> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _collectionName = configuration["Qdrant:CollectionName"] 
            ?? throw new InvalidOperationException("Qdrant:CollectionName configuration is required");
        _vectorSize = configuration.GetValue<int>("Qdrant:VectorSize");
        _distance = configuration["Qdrant:Distance"] 
            ?? throw new InvalidOperationException("Qdrant:Distance configuration is required");
    }

    private HttpClient GetHttpClient() => _httpClientFactory.CreateClient("Qdrant");

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var checkResponse = await GetHttpClient().GetAsync($"/collections/{_collectionName}", cancellationToken);
        
        if (checkResponse.StatusCode == HttpStatusCode.NotFound)
        {
            var createRequest = new QdrantCreateCollectionRequest
            {
                Vectors = new QdrantVectorParams
                {
                    Size = _vectorSize,
                    Distance = _distance
                }
            };

            var createResponse = await GetHttpClient().PutAsJsonAsync(
                $"/collections/{_collectionName}", 
                createRequest, 
                cancellationToken);
            
            createResponse.EnsureSuccessStatusCode();
            _logger.LogInformation("Qdrant collection '{Collection}' created.", _collectionName);
        }
        else
        {
            checkResponse.EnsureSuccessStatusCode();
            _logger.LogInformation("Qdrant collection '{Collection}' already exists.", _collectionName);
        }
    }

    public async Task UpsertChunksAsync(
        IReadOnlyList<DocumentChunk> chunks, 
        IReadOnlyDictionary<string, float[]> embeddings, 
        CancellationToken cancellationToken = default)
    {
        var points = new List<QdrantPoint>();

        foreach (var chunk in chunks)
        {
            if (!embeddings.TryGetValue(chunk.Id, out var vector))
            {
                _logger.LogWarning("Embedding not found for chunk {ChunkId}, skipping", chunk.Id);
                continue;
            }

            var point = new QdrantPoint
            {
                Id = chunk.Id,
                Vector = vector,
                Payload = new Dictionary<string, object>
                {
                    ["documentId"] = chunk.DocumentId.ToString(),
                    ["text"] = chunk.Text,
                    ["sourceFile"] = chunk.SourceFile,
                    ["sourcePath"] = chunk.SourcePath,
                    ["title"] = chunk.Title ?? string.Empty,
                    ["section"] = chunk.Section ?? string.Empty,
                    ["chunkIndex"] = chunk.ChunkIndex,
                    ["indexedAtUtc"] = chunk.IndexedAtUtc.ToString("O")
                }
            };

            points.Add(point);
        }

        if (points.Count == 0)
        {
            _logger.LogWarning("No valid points to upsert");
            return;
        }

        _logger.LogInformation("Upserting {Count} points to Qdrant collection '{Collection}'.", points.Count, _collectionName);

        var payload = new { points };
        var response = await GetHttpClient().PutAsJsonAsync(
            $"/collections/{_collectionName}/points?wait=true", 
            payload, 
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var filter = new
        {
            filter = new
            {
                must = new[]
                {
                    new { key = "documentId", match = new { value = documentId.ToString() } }
                }
            }
        };

        var response = await GetHttpClient().PostAsJsonAsync(
            $"/collections/{_collectionName}/points/delete?wait=true", 
            filter, 
            cancellationToken);

        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Deleted Qdrant points for document {DocumentId}.", documentId);
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchSimilarChunksAsync(
        float[] queryVector, 
        int topK, 
        float? minScore = null, 
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new QdrantSearchRequest
        {
            Vector = queryVector,
            Limit = topK,
            ScoreThreshold = minScore,
            WithPayload = true,
            WithVector = false
        };

        var response = await GetHttpClient().PostAsJsonAsync(
            $"/collections/{_collectionName}/points/search",
            searchRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var searchResponse = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken);
        if (searchResponse?.Result == null)
        {
            _logger.LogWarning("Empty search response from Qdrant");
            return Array.Empty<RetrievedChunk>();
        }

        var chunks = new List<RetrievedChunk>();
        foreach (var result in searchResponse.Result)
        {
            try
            {
                var payload = result.Payload;
                var chunk = new RetrievedChunk(
                    Id: result.Id,
                    DocumentId: Guid.Parse(GetStringValue(payload, "documentId")!),
                    Text: GetStringValue(payload, "text")!,
                    SourceFile: GetStringValue(payload, "sourceFile")!,
                    SourcePath: GetStringValue(payload, "sourcePath")!,
                    Title: GetStringValue(payload, "title"),
                    Section: GetStringValue(payload, "section"),
                    ChunkIndex: GetInt32Value(payload, "chunkIndex"),
                    IndexedAtUtc: DateTime.Parse(GetStringValue(payload, "indexedAtUtc")!),
                    Score: result.Score
                );
                chunks.Add(chunk);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse search result {Id}", result.Id);
            }
        }

        _logger.LogInformation("Found {Count} similar chunks with scores >= {MinScore}", 
            chunks.Count, minScore ?? 0f);
        
        return chunks;
    }

    private static string? GetStringValue(Dictionary<string, object> payload, string key)
    {
        if (!payload.ContainsKey(key))
            return null;

        var value = payload[key];
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Null ||
                jsonElement.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                return null;
            
            var str = jsonElement.GetString();
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        return value?.ToString();
    }

    private static int GetInt32Value(Dictionary<string, object> payload, string key)
    {
        var value = payload[key];
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.GetInt32();
        }

        return Convert.ToInt32(value);
    }
}
