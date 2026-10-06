# Pipeline 2 Quick Start Guide

Get up and running with the complete TechnicalDocsQa RAG system in minutes!

## 🚀 Quick Start (3 Steps)

### Step 1: Start the System

```bash
cd /Users/francescomacchetti/Documents/Projects/NSAQuestionAnswerer
make start
```

Or:
```bash
docker-compose up -d
```

**⏳ First startup**: Wait 5-10 minutes for Ollama to download models:
- `nomic-embed-text` (~270MB) - for embeddings
- `llama3.1` (~4.7GB) - for chat completions

Monitor progress:
```bash
docker-compose logs -f ollama
```

### Step 2: Verify All Services Are Running

```bash
make status
```

You should see:
- ✅ `qdrant` - running
- ✅ `ollama` - running
- ✅ `api` - running
- ✅ `blazor` - running

Check models are downloaded:
```bash
make check
```

### Step 3: Open the UI

Navigate to: **http://localhost:5173**

---

## 🎨 Using the Web Interface

### Upload a Document

1. Click **Upload** in the navigation
2. Choose a Markdown file (`.md`)
3. Click **Upload**
4. Wait for indexing (usually 10-30 seconds)

### Ask a Question

1. Click **Ask Question** in the navigation
2. Type your question (e.g., "What is this document about?")
3. Click **Ask Question**
4. Wait 5-15 seconds for the answer
5. Review the answer and source citations!

### Manage Documents

1. Click **Documents** in the navigation
2. View all uploaded documents with their status
3. **Reindex** if a document failed or needs updating
4. **Delete** documents you no longer need

---

## 🧪 Test Pipeline 2 (CLI)

### Option 1: Automated Test
```bash
make test-pipeline2
```

This will:
1. Upload `example-document.md`
2. Wait for indexing
3. Ask a question
4. Display the answer with sources

### Option 2: Manual Testing

**1. Upload a document:**
```bash
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "file=@example-document.md"
```

**2. Check indexing status:**
```bash
curl "http://localhost:5000/api/documents" | jq '.[0] | {fileName, status}'
```

Wait until `status` is `"Indexed"`.

**3. Ask a question:**
```bash
curl -X POST "http://localhost:5000/api/questions/ask" \
  -H "Content-Type: application/json" \
  -d '{"question": "What is this document about?"}' | jq .
```

---

## 📊 Service URLs

| Service | URL | Description |
|---------|-----|-------------|
| **Blazor UI** | http://localhost:5173 | Main web interface |
| **API** | http://localhost:5000 | REST API |
| **Swagger** | http://localhost:5000/swagger | API documentation |
| **Qdrant** | http://localhost:6333 | Vector database |
| **Qdrant Dashboard** | http://localhost:6333/dashboard | Qdrant UI |
| **Ollama** | http://localhost:11434 | AI models |

---

## 🐛 Troubleshooting

### "Services not starting"
```bash
# Check Docker is running
docker ps

# View logs
docker-compose logs
```

### "Ollama models not downloading"
```bash
# Check Ollama logs
docker-compose logs ollama

# Manually pull models
docker exec -it ollama ollama pull nomic-embed-text
docker exec -it ollama ollama pull llama3.1
```

### "No relevant information found"
- Ensure documents are indexed (`status = "Indexed"`)
- Check documents at: http://localhost:5173/documents
- Try rephrasing your question

### "Blazor UI can't connect to API"
```bash
# Check API is running
curl http://localhost:5000/api/documents

# Check CORS (should see API response, not CORS error)
```

### "Answer generation is slow"
- Normal on CPU: 5-15 seconds
- For faster performance: Use GPU-enabled Docker
- Check Ollama logs for progress

---

## 🔧 Configuration

### RAG Settings (API)

Edit `TechnicalDocsQa.Api/appsettings.json`:

```json
{
  "Rag": {
    "TopK": 5,                      // Number of chunks to retrieve
    "MinScore": 0.25,               // Minimum similarity score (0-1)
    "MaxContextCharacters": 12000,  // Max context length
    "Temperature": 0.2,             // LLM temperature (0-1)
    "RequireSources": true          // Require sources to answer
  }
}
```

**Tuning tips:**
- ⬆️ Increase `TopK` for more context
- ⬇️ Lower `MinScore` for more lenient matching
- ⬆️ Increase `Temperature` for more creative answers
- ⬇️ Decrease `MaxContextCharacters` for faster responses

### API URL (Blazor)

Edit `TechnicalDocsQa.Blazor/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

---

## 📚 Example Questions

Try these questions with `example-document.md`:

1. "What is this document about?"
2. "What are the main features?"
3. "How do I get started?"
4. "What are the prerequisites?"
5. "Explain the architecture"

---

## 🛑 Stopping the System

```bash
make stop
```

Or:
```bash
docker-compose down
```

To also **remove all data** (documents, vectors, etc.):
```bash
make clean
```

---

## 📖 More Information

- **[PIPELINE2.md](./PIPELINE2.md)**: Complete technical documentation
- **[PIPELINE2_IMPLEMENTATION.md](./PIPELINE2_IMPLEMENTATION.md)**: Implementation details
- **[README.md](./README.md)**: Full system documentation

---

## ✅ Quick Verification Checklist

After starting the system, verify:

- [ ] Ollama models downloaded (check with `make check`)
- [ ] All services running (check with `make status`)
- [ ] Blazor UI loads at http://localhost:5173
- [ ] Swagger UI loads at http://localhost:5000/swagger
- [ ] Can upload a document via UI
- [ ] Document status changes to "Indexed"
- [ ] Can ask a question and get an answer
- [ ] Answer includes source citations

---

## 🎉 Success!

If you can:
1. ✅ Upload a document
2. ✅ See it indexed
3. ✅ Ask a question
4. ✅ Get an answer with sources

**Pipeline 2 is working perfectly!** 🚀

Explore the UI, try different questions, and upload your own technical documentation!
