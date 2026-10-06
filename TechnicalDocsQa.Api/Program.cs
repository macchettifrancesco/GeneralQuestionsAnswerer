using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Application.Services;
using TechnicalDocsQa.Api.Application.Background;
using TechnicalDocsQa.Api.Infrastructure.Ollama;
using TechnicalDocsQa.Api.Infrastructure.Qdrant;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001", 
                "http://localhost:5173",  // Blazor dev server & Docker
                "http://localhost:80",
                "http://blazor:80"        // Docker internal
              )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register storage and registry services
builder.Services.AddSingleton<IDocumentStorageService, DocumentStorageService>();
builder.Services.AddSingleton<IDocumentRegistryService, DocumentRegistryService>();

// Register document processing services
builder.Services.AddSingleton<IMarkdownParserService, MarkdownParserService>();
builder.Services.AddSingleton<IChunkingService, ChunkingService>();

// Register indexing service as Singleton (stateless)
builder.Services.AddSingleton<IDocumentIndexingService, DocumentIndexingService>();

// Register indexing queue
builder.Services.AddSingleton<IIndexingQueue, IndexingQueue>();

// Register background service
builder.Services.AddHostedService<DocumentIndexingBackgroundService>();

// Configure HTTP clients
builder.Services.AddHttpClient("Ollama", client =>
{
    var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient("OllamaChat", client =>
{
    var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient("Qdrant", client =>
{
    var qdrantBaseUrl = builder.Configuration["Qdrant:BaseUrl"] ?? "http://localhost:6333";
    client.BaseAddress = new Uri(qdrantBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});

// Register services as Singleton (all stateless, use IHttpClientFactory)
builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingClient>();
builder.Services.AddSingleton<IVectorStoreService, QdrantVectorStore>();
builder.Services.AddSingleton<IChatCompletionService, OllamaChatClient>();

// Register RAG services as Singleton (stateless)
builder.Services.AddSingleton<IRagPromptBuilder, RagPromptBuilder>();
builder.Services.AddSingleton<IQuestionAnsweringService, QuestionAnsweringService>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for Docker deployment
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS
app.UseCors("AllowBlazorClient");

// Note: HTTPS redirection disabled for Docker internal communication
// Use a reverse proxy (nginx, traefik) for HTTPS termination in production
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();
