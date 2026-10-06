using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using TechnicalDocsQa.Blazor.Models;

namespace TechnicalDocsQa.Blazor.Services;

public sealed class DocumentApiClient
{
    private readonly HttpClient _httpClient;

    public DocumentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DocumentRecord>> GetDocumentsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<DocumentRecord>>("/documents")
            ?? new List<DocumentRecord>();
    }

    public async Task<DocumentRecord> GetDocumentAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<DocumentRecord>($"/documents/{id}")
            ?? throw new InvalidOperationException($"Document {id} not found");
    }

    public async Task<UploadDocumentResponse> UploadDocumentAsync(IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
        content.Add(fileContent, "file", file.Name);

        var response = await _httpClient.PostAsync("/documents/upload", content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<UploadDocumentResponse>()
            ?? throw new InvalidOperationException("Empty response from API");
    }

    public async Task DeleteDocumentAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/documents/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task ReindexDocumentAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"/documents/{id}/reindex", null);
        response.EnsureSuccessStatusCode();
    }
}
