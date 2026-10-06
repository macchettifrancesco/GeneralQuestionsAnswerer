using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class QuestionAnsweringService : IQuestionAnsweringService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IRagPromptBuilder _promptBuilder;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuestionAnsweringService> _logger;
    
    private readonly int _topK;
    private readonly float _minScore;
    private readonly bool _requireSources;

    public QuestionAnsweringService(
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        IRagPromptBuilder promptBuilder,
        IChatCompletionService chatCompletionService,
        IConfiguration configuration,
        ILogger<QuestionAnsweringService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _promptBuilder = promptBuilder;
        _chatCompletionService = chatCompletionService;
        _configuration = configuration;
        _logger = logger;
        
        _topK = configuration.GetValue<int>("Rag:TopK", 5);
        _minScore = configuration.GetValue<float>("Rag:MinScore", 0.25f);
        _requireSources = configuration.GetValue<bool>("Rag:RequireSources", true);
    }

    public async Task<AskQuestionResponse> AnswerQuestionAsync(
        string question, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing question: {Question}", question);

        // Step 1: Embed the question
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            question, 
            cancellationToken);

        // Step 2: Search for similar chunks
        var retrievedChunks = await _vectorStoreService.SearchSimilarChunksAsync(
            questionEmbedding,
            _topK,
            _minScore,
            cancellationToken);

        _logger.LogInformation("Retrieved {Count} relevant chunks", retrievedChunks.Count);

        // Step 3: Check if we have sources (if required)
        if (_requireSources && retrievedChunks.Count == 0)
        {
            return new AskQuestionResponse(
                Answer: "Non ho trovato informazioni sufficienti nei documenti indicizzati per rispondere a questa domanda.",
                Sources: Array.Empty<RagSource>(),
                TokensUsed: 0,
                SourcesFound: 0,
                AnsweredAtUtc: DateTime.UtcNow);
        }

        // Step 4: Build RAG prompt
        var prompt = _promptBuilder.BuildPrompt(question, retrievedChunks);

        // Step 5: Generate answer
        var chatResult = await _chatCompletionService.GenerateAnswerAsync(
            prompt, 
            cancellationToken);

        // Step 6: Build sources list
        var sources = retrievedChunks.Select(chunk => new RagSource(
            DocumentId: chunk.DocumentId,
            SourceFile: chunk.SourceFile,
            SourcePath: chunk.SourcePath,
            Title: chunk.Title,
            Section: chunk.Section,
            ChunkIndex: chunk.ChunkIndex,
            Score: chunk.Score
        )).ToList();

        _logger.LogInformation(
            "Question answered successfully. Sources: {SourceCount}, Tokens: {TokenCount}",
            sources.Count,
            chatResult.TokensUsed);

        return new AskQuestionResponse(
            Answer: chatResult.Answer,
            Sources: sources,
            TokensUsed: chatResult.TokensUsed,
            SourcesFound: retrievedChunks.Count,
            AnsweredAtUtc: DateTime.UtcNow);
    }
}
