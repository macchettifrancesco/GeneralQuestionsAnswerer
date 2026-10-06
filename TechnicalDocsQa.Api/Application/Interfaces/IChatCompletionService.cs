using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IChatCompletionService
{
    Task<ChatCompletionResult> GenerateAnswerAsync(
        string prompt, 
        CancellationToken cancellationToken = default);
}
