using System.Net.Http.Json;
using TechnicalDocsQa.Blazor.Models;

namespace TechnicalDocsQa.Blazor.Services;

public sealed class QuestionApiClient
{
    private readonly HttpClient _httpClient;

    public QuestionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AskQuestionResponse> AskQuestionAsync(string question)
    {
        var request = new AskQuestionRequest(question);
        var response = await _httpClient.PostAsJsonAsync("/api/Questions/ask", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AskQuestionResponse>()
            ?? throw new InvalidOperationException("Empty response from API");
    }
}
