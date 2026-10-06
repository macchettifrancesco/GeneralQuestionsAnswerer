using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IQuestionAnsweringService
{
    Task<AskQuestionResponse> AnswerQuestionAsync(
        string question, 
        CancellationToken cancellationToken = default);
}
