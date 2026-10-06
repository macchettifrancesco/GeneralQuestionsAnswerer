using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Interfaces;

public interface IRagPromptBuilder
{
    string BuildPrompt(string question, IReadOnlyList<RetrievedChunk> chunks);
}
