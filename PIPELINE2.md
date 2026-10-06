# Pipeline 2 - Question Answering & Blazor UI

Pipeline 2 adds intelligent question-answering capabilities and a user-friendly Blazor WebAssembly interface to the TechnicalDocsQa system.

## 🎯 Overview

**Pipeline 2** extends the document indexing system (Pipeline 1) with:

1. **RAG Question Answering Backend**: Ask questions about indexed documents and receive AI-generated answers with source citations
2. **Blazor WebAssembly UI**: Modern, responsive web interface for document management and Q&A

## 🏗️ Architecture

### Backend Components (TechnicalDocsQa.Api)

#### New Domain Models
- **RetrievedChunk**: Represents a document chunk returned from vector search with relevance score
- **AskQuestionRequest**: Question submission payload
- **AskQuestionResponse**: Answer with sources, token count, and metadata
- **RagSource**: Source document reference with score
- **ChatCompletionResult**: Result from Ollama chat completion

#### New Interfaces
- **IQuestionAnsweringService**: Orchestrates the RAG pipeline
- **IRagPromptBuilder**: Builds context-grounded prompts for the LLM
- **IChatCompletionService**: Generates answers using Ollama chat

#### Infrastructure Extensions
- **QdrantSearchRequest/Response**: Semantic search models
- **IVectorStoreService.SearchSimilarChunksAsync**: Vector similarity search method
- **OllamaChatClient**: HTTP client for Ollama chat API (llama3.1)
- **OllamaChatRequest/Response**: Chat completion models

#### Application Services
- **RagPromptBuilder**: Constructs prompts with retrieved context
- **QuestionAnsweringService**: Main RAG orchestration service
  - Embeds the question using `nomic-embed-text`
  - Searches Qdrant for top-K similar chunks
  - Builds a grounded prompt with context
  - Generates answer using `llama3.1`
  - Returns answer with source citations

#### Controllers
- **QuestionsController**: `POST /api/questions/ask` endpoint

### Frontend Components (TechnicalDocsQa.Blazor)

#### Pages
- **Home.razor**: Landing page with feature overview
- **Ask.razor**: Question submission and answer display with sources
- **Documents.razor**: Document list with management actions (delete, reindex)
- **UploadDocument.razor**: File upload interface

#### Services
- **QuestionApiClient**: HTTP client for Q&A endpoint
- **DocumentApiClient**: HTTP client for document management

#### Models
- DTOs matching the API contracts (questions, documents, sources)

#### Layout
- **MainLayout.razor**: Application shell
- **NavMenu.razor**: Navigation component

## 🔧 Configuration

### API Configuration (appsettings.json)

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text",
    "ChatModel": "llama3.1"
  },
  "Rag": {
    "TopK": 5,
    "MinScore": 0.25,
    "MaxContextCharacters": 12000,
    "Temperature": 0.2,
    "RequireSources": true
  }
}
```

**RAG Configuration:**
- **TopK**: Number of document chunks to retrieve (default: 5)
- **MinScore**: Minimum similarity score (0-1) to include a chunk (default: 0.25)
- **MaxContextCharacters**: Maximum characters to include in prompt (default: 12,000)
- **Temperature**: LLM temperature for answer generation (default: 0.2 for focused answers)
- **RequireSources**: Require at least one source to generate an answer (default: true)

### Blazor Configuration (wwwroot/appsettings.json)

```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

## 🚀 Deployment

### Docker Compose

The system is fully containerized. Run:

```bash
# Start all services (includes Blazor UI)
docker-compose up -d

# View logs
docker-compose logs -f
```

**Services:**
- **API**: http://localhost:5000 (Swagger: http://localhost:5000/swagger)
- **Blazor UI**: http://localhost:5173
- **Qdrant**: http://localhost:6333
- **Ollama**: http://localhost:11434

### Models Downloaded

On first startup, Ollama automatically pulls:
- `nomic-embed-text` (~270MB) - for embeddings
- `llama3.1` (~4.7GB) - for chat completions

**Note:** First startup takes 5-10 minutes depending on network speed.

## 📡 API Endpoints

### Questions

#### POST /api/questions/ask

Ask a question about indexed documents.

**Request:**
```json
{
  "question": "How do I configure authentication?"
}
```

**Response:**
```json
{
  "answer": "To configure authentication...",
  "sources": [
    {
      "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "sourceFile": "auth-guide.md",
      "sourcePath": "docs/auth-guide.md",
      "title": "Authentication Guide",
      "section": "Configuration",
      "chunkIndex": 2,
      "score": 0.873
    }
  ],
  "tokensUsed": 1543,
  "sourcesFound": 3,
  "answeredAtUtc": "2026-05-21T10:30:00Z"
}
```

## 🎨 UI Features

### Ask Question Page
- Text area for question input
- Real-time answer generation
- Source citations with relevance scores
- Token usage and metadata display

### Documents Page
- List of all uploaded documents
- Status indicators (Uploaded, Indexing, Indexed, Failed)
- Reindex button for failed/outdated documents
- Delete functionality
- File size and upload timestamp display

### Upload Page
- Drag-and-drop or file picker
- File validation (Markdown only, max 10MB)
- Upload progress indication
- Success confirmation with document details

## 🔄 RAG Pipeline Flow

```
User Question
    ↓
[1] Embed Question (nomic-embed-text)
    ↓
[2] Vector Search (Qdrant) → Top 5 chunks with score ≥ 0.25
    ↓
[3] Build RAG Prompt (context + question)
    ↓
[4] Generate Answer (llama3.1)
    ↓
[5] Return Answer + Sources
```

### Prompt Template

The system uses this prompt structure:

```
You are a helpful technical documentation assistant. Answer the user's question based ONLY on the provided context from the technical documentation.

RULES:
- Answer directly and concisely
- Use ONLY information from the context below
- If the context doesn't contain enough information, say so clearly
- Cite specific sections or documents when possible
- Do not make up information or use external knowledge

CONTEXT FROM DOCUMENTATION:
[Source: auth-guide.md - Configuration (Score: 0.873)]
{chunk text}

---

[Source: api-docs.md - Authentication (Score: 0.756)]
{chunk text}

USER QUESTION:
{user question}

ANSWER:
```

## 🧪 Testing Pipeline 2

### 1. Upload a Document
```bash
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "file=@example-document.md"
```

### 2. Wait for Indexing
Check status at http://localhost:5173/documents or:
```bash
curl "http://localhost:5000/api/documents"
```

### 3. Ask a Question
Via UI: http://localhost:5173/ask

Or via API:
```bash
curl -X POST "http://localhost:5000/api/questions/ask" \
  -H "Content-Type: application/json" \
  -d '{"question": "What is this document about?"}'
```

## 📊 Performance Considerations

### Embedding
- **Model**: nomic-embed-text (768 dimensions)
- **Speed**: ~50-100 tokens/sec on CPU, faster on GPU
- **Typical question**: 10-20 tokens → ~200ms

### Vector Search
- **Qdrant**: In-memory index, typically <50ms for TopK=5
- **HNSW**: Efficient approximate nearest neighbor search

### Chat Generation
- **Model**: llama3.1 (8B parameters)
- **Speed**: ~20-30 tokens/sec on CPU, ~60-100 tokens/sec on GPU
- **Typical answer**: 100-300 tokens → 5-15 seconds

**Total latency**: 5-20 seconds for a typical question-answer cycle.

## 🐛 Troubleshooting

### "No relevant information found"
- Check documents are indexed (status = "Indexed")
- Lower `Rag:MinScore` (default 0.25)
- Increase `Rag:TopK` (default 5)
- Verify question phrasing matches document terminology

### Blazor UI can't connect to API
- Verify API is running: `docker-compose ps`
- Check CORS configuration in `Program.cs`
- Ensure `ApiBaseUrl` in Blazor config matches API URL
- Check browser console for errors

### Ollama model not found
- Wait for initial model download (check logs: `docker-compose logs ollama`)
- Manually pull models:
  ```bash
  docker exec -it ollama ollama pull nomic-embed-text
  docker exec -it ollama ollama pull llama3.1
  ```

### Slow answer generation
- Normal on CPU (~10-15 seconds)
- For faster performance, use GPU-enabled Docker image
- Consider reducing `Rag:MaxContextCharacters` to shorten prompts

## 🔐 Security Notes

- **No Authentication**: This is a demo system. Add authentication before production use.
- **CORS**: Configured for local development. Restrict origins in production.
- **File Upload**: Validates file types and size. Consider additional scanning in production.

## 📚 Additional Resources

- [Pipeline 1 Documentation](./README.md) - Document indexing system
- [Ollama Documentation](https://ollama.ai/docs)
- [Qdrant Documentation](https://qdrant.tech/documentation/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

## ✅ Success Criteria

Pipeline 2 is complete when:

1. ✅ `POST /api/questions/ask` returns answers with sources
2. ✅ Blazor UI accessible at http://localhost:5173
3. ✅ All 4 pages render correctly (Home, Ask, Documents, Upload)
4. ✅ CORS allows Blazor → API communication
5. ✅ Semantic search retrieves relevant chunks from Qdrant
6. ✅ Ollama chat generates coherent answers with llama3.1
7. ✅ Source citations include document names, sections, and scores
8. ✅ Docker Compose starts all services successfully

---

**Pipeline 2 Status**: ✅ **COMPLETE**
