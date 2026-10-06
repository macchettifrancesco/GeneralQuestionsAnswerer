using System.Net.Http.Json;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Infrastructure.Ollama;

public sealed class OllamaChatClient : IChatCompletionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaChatClient> _logger;
    private readonly string _chatModel;
    private readonly float _temperature;

    public OllamaChatClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaChatClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _chatModel = configuration["Ollama:ChatModel"] 
            ?? throw new InvalidOperationException("Ollama:ChatModel configuration is required");
        _temperature = configuration.GetValue<float>("Rag:Temperature", 0.2f);
    }

    public async Task<ChatCompletionResult> GenerateAnswerAsync(
        string prompt, 
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = _chatModel,
            Messages = new List<OllamaChatMessage>
            {
                new() { Role = "user", Content = prompt }
            },
            Stream = false,
            Options = new OllamaChatOptions
            {
                Temperature = _temperature
            }
        };

        _logger.LogInformation("Generating chat completion with model {Model}", _chatModel);

        var httpClient = _httpClientFactory.CreateClient("OllamaChat");
        var response = await httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Ollama chat API");

        if (!chatResponse.Done || chatResponse.Message == null)
        {
            throw new InvalidOperationException("Incomplete response from Ollama chat API");
        }

        var totalTokens = chatResponse.PromptEvalCount + chatResponse.EvalCount;
        
        _logger.LogInformation(
            "Chat completion generated. Prompt tokens: {PromptTokens}, Completion tokens: {CompletionTokens}, Total: {TotalTokens}",
            chatResponse.PromptEvalCount,
            chatResponse.EvalCount,
            totalTokens);

        return new ChatCompletionResult(
            Answer: chatResponse.Message.Content,
            TokensUsed: totalTokens);
    }
}
