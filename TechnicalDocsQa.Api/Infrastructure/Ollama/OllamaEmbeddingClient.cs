using System.Net.Http.Json;
using TechnicalDocsQa.Api.Application.Interfaces;

namespace TechnicalDocsQa.Api.Infrastructure.Ollama;

public sealed class OllamaEmbeddingClient : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingClient> _logger;

    public OllamaEmbeddingClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaEmbeddingClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _model = configuration["Ollama:EmbeddingModel"]
            ?? throw new InvalidOperationException("Ollama:EmbeddingModel configuration is missing.");
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding for text of length {Length} using model '{Model}'.", text.Length, _model);

        var httpClient = _httpClientFactory.CreateClient("Ollama");
        var request = new { model = _model, prompt = text };
        var response = await httpClient.PostAsJsonAsync("/api/embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);

        if (result?.Embedding is null || result.Embedding.Length == 0)
            throw new InvalidOperationException("Ollama returned an empty embedding vector.");

        _logger.LogDebug("Embedding generated: {Dimensions} dimensions.", result.Embedding.Length);

        return result.Embedding;
    }
}
