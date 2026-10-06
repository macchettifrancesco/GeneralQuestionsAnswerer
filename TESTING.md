# 🧪 Testing Guide - TechnicalDocsQa.Api

Guida completa al testing del sistema RAG Pipeline 1.

## 📋 Indice

- [Test rapidi](#test-rapidi)
- [Test manuali](#test-manuali)
- [Test automatizzati](#test-automatizzati)
- [Verifica end-to-end](#verifica-end-to-end)
- [Performance testing](#performance-testing)

---

## ⚡ Test rapidi

### Test 1: Sistema è avviato correttamente?

```bash
# Verifica che tutti i servizi siano UP e healthy
docker-compose ps

# Output atteso:
# NAME                    STATUS         PORTS
# technicaldocsqa-api     Up (healthy)   0.0.0.0:5000->8080/tcp
# ollama                  Up (healthy)   0.0.0.0:11434->11434/tcp
# qdrant                  Up (healthy)   0.0.0.0:6333->6333/tcp
```

### Test 2: API risponde?

```bash
curl http://localhost:5000/documents

# Output atteso: [] (lista vuota se nessun documento)
```

### Test 3: Ollama funziona?

```bash
docker exec ollama ollama list

# Output atteso:
# NAME                    ID              SIZE
# nomic-embed-text:latest abc123def456    270 MB
```

### Test 4: Qdrant è pronto?

```bash
curl http://localhost:6333/collections

# Output atteso: {"result":{"collections":[]}}
```

---

## 🔬 Test manuali

### Test Upload Documento

#### 1. Prepara un documento di test

```bash
cat > test-simple.md << 'EOF'
# Test Document

## Section 1

This is the first section with some content.

## Section 2

This is the second section with more content.
EOF
```

#### 2. Upload del documento

```bash
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@test-simple.md" | jq
```

**Output atteso:**
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "test-simple.md",
  "status": "Uploaded",
  "chunksCreated": null,
  "message": "Document uploaded. Indexing has been queued."
}
```

#### 3. Verifica stato indicizzazione

```bash
# Attendi 5-10 secondi per l'indicizzazione
sleep 10

# Verifica lo stato
DOCUMENT_ID="<copia-il-documentId-dal-passo-2>"
curl "http://localhost:5000/documents/$DOCUMENT_ID" | jq
```

**Output atteso (stato Indexed):**
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "originalFileName": "test-simple.md",
  "status": "Indexed",
  "chunksCount": 2,
  "uploadedAtUtc": "2026-05-21T10:00:00Z",
  "indexedAtUtc": "2026-05-21T10:00:05Z",
  "errorMessage": null
}
```

#### 4. Verifica chunk in Qdrant

```bash
# Verifica collection creata
curl "http://localhost:6333/collections/technical_docs" | jq

# Conta punti nella collection
curl "http://localhost:6333/collections/technical_docs/points/count" | jq

# Output atteso:
# {"result":{"count":2}}
```

#### 5. Verifica payload chunk

```bash
# Scroll primi 10 punti
curl -X POST "http://localhost:6333/collections/technical_docs/points/scroll" \
  -H "Content-Type: application/json" \
  -d '{
    "limit": 10,
    "with_payload": true,
    "with_vector": false
  }' | jq
```

**Verifica che il payload contenga:**
- `documentId`
- `text` (contenuto del chunk)
- `sourceFile`
- `title`
- `section`
- `chunkIndex`

---

### Test Duplicati

#### 1. Carica lo stesso documento due volte

```bash
# Primo upload
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@test-simple.md" | jq

# Secondo upload (stesso file)
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@test-simple.md" | jq
```

**Output atteso (secondo upload):**
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "test-simple.md",
  "status": "Indexed",
  "chunksCreated": 2,
  "message": "Document already exists and is already indexed."
}
```

Verifica che `documentId` sia lo stesso e status code sia `200 OK` (non `202 Accepted`).

---

### Test Eliminazione

#### 1. Elimina un documento

```bash
DOCUMENT_ID="<copia-documentId>"
curl -X DELETE "http://localhost:5000/documents/$DOCUMENT_ID"

# Output atteso: 204 No Content (nessun body)
```

#### 2. Verifica eliminazione

```bash
# Verifica stato documento
curl "http://localhost:5000/documents/$DOCUMENT_ID" | jq

# Status deve essere "Deleted"
```

#### 3. Verifica chunk rimossi da Qdrant

```bash
# Conta punti (dovrebbe diminuire)
curl "http://localhost:6333/collections/technical_docs/points/count" | jq
```

---

### Test Re-indicizzazione

#### 1. Re-indicizza un documento

```bash
DOCUMENT_ID="<copia-documentId>"
curl -X POST "http://localhost:5000/documents/$DOCUMENT_ID/reindex" | jq

# Output atteso: 202 Accepted
```

#### 2. Verifica stato cambia

```bash
# Subito dopo
curl "http://localhost:5000/documents/$DOCUMENT_ID" | jq
# Status: "Uploaded"

# Dopo 10 secondi
sleep 10
curl "http://localhost:5000/documents/$DOCUMENT_ID" | jq
# Status: "Indexed"
```

---

### Test Validazione

#### 1. File non .md

```bash
echo "test" > test.txt
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@test.txt"

# Output atteso: 400 Bad Request
# Message: "Only .md files are accepted."
```

#### 2. File vuoto

```bash
touch empty.md
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@empty.md"

# Output atteso: 400 Bad Request
# Message: "No file provided or file is empty."
```

#### 3. File troppo grande (>10MB)

```bash
# Crea file 11MB
dd if=/dev/zero of=large.md bs=1M count=11
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@large.md"

# Output atteso: 400 Bad Request
# Message: "File exceeds maximum allowed size of 10 MB."
```

---

## 🤖 Test automatizzati

### Script di test completo

```bash
#!/bin/bash
# test-suite.sh - Test suite completo

set -e

echo "🧪 Starting test suite..."

# Test 1: Health check
echo "Test 1: Health check"
curl -f http://localhost:5000/documents > /dev/null
echo "✅ API is responding"

# Test 2: Upload
echo ""
echo "Test 2: Upload document"
cat > test-doc.md << 'EOF'
# Test
Content here.
EOF

response=$(curl -s -X POST "http://localhost:5000/documents/upload" -F "file=@test-doc.md")
document_id=$(echo "$response" | jq -r '.documentId')
echo "✅ Document uploaded: $document_id"

# Test 3: Wait for indexing
echo ""
echo "Test 3: Wait for indexing"
sleep 10

status=$(curl -s "http://localhost:5000/documents/$document_id" | jq -r '.status')
if [ "$status" == "Indexed" ]; then
  echo "✅ Document indexed successfully"
else
  echo "❌ Document not indexed (status: $status)"
  exit 1
fi

# Test 4: Verify Qdrant
echo ""
echo "Test 4: Verify Qdrant"
count=$(curl -s "http://localhost:6333/collections/technical_docs/points/count" | jq -r '.result.count')
if [ "$count" -gt 0 ]; then
  echo "✅ Qdrant has $count points"
else
  echo "❌ Qdrant is empty"
  exit 1
fi

# Test 5: Delete
echo ""
echo "Test 5: Delete document"
curl -s -X DELETE "http://localhost:5000/documents/$document_id" > /dev/null
echo "✅ Document deleted"

# Cleanup
rm -f test-doc.md

echo ""
echo "✅ All tests passed!"
```

Esegui:
```bash
chmod +x test-suite.sh
./test-suite.sh
```

---

## 🔍 Verifica end-to-end

### Scenario completo

```bash
#!/bin/bash
# end-to-end-test.sh

echo "📊 End-to-End Test"
echo ""

# 1. Crea documento complesso
cat > e2e-test.md << 'EOF'
---
title: E2E Test Document
author: Test Suite
---

# Introduction

This document tests the complete RAG pipeline.

## Architecture

The system has three components:
- API
- Ollama
- Qdrant

## Features

### Parsing

YAML frontmatter is removed automatically.

### Chunking

Text is divided into semantic chunks.

### Embedding

Each chunk gets a 768-dimensional vector.

## Conclusion

Testing complete!
EOF

# 2. Upload
echo "1. Uploading document..."
response=$(curl -s -X POST "http://localhost:5000/documents/upload" -F "file=@e2e-test.md")
doc_id=$(echo "$response" | jq -r '.documentId')
echo "   Document ID: $doc_id"

# 3. Wait for indexing
echo ""
echo "2. Waiting for indexing (15 seconds)..."
sleep 15

# 4. Check status
echo ""
echo "3. Checking document status..."
doc=$(curl -s "http://localhost:5000/documents/$doc_id")
status=$(echo "$doc" | jq -r '.status')
chunks=$(echo "$doc" | jq -r '.chunksCount')
echo "   Status: $status"
echo "   Chunks: $chunks"

# 5. Verify Qdrant
echo ""
echo "4. Verifying Qdrant..."
qdrant_count=$(curl -s "http://localhost:6333/collections/technical_docs/points/count" | jq -r '.result.count')
echo "   Total points in Qdrant: $qdrant_count"

# 6. Search in Qdrant (simulate)
echo ""
echo "5. Simulating vector search..."
curl -s -X POST "http://localhost:6333/collections/technical_docs/points/scroll" \
  -H "Content-Type: application/json" \
  -d '{
    "limit": 3,
    "with_payload": true,
    "with_vector": false,
    "filter": {
      "must": [
        {
          "key": "documentId",
          "match": {
            "value": "'"$doc_id"'"
          }
        }
      ]
    }
  }' | jq '.result.points[] | {id, payload: {text: .payload.text, section: .payload.section}}'

# 7. Cleanup
echo ""
echo "6. Cleaning up..."
curl -s -X DELETE "http://localhost:5000/documents/$doc_id" > /dev/null
rm -f e2e-test.md
echo "   Cleanup complete"

echo ""
echo "✅ End-to-End test completed successfully!"
```

---

## 📈 Performance testing

### Test carico

```bash
#!/bin/bash
# load-test.sh - Carica multipli documenti

echo "📈 Load Test - Uploading 10 documents"

for i in {1..10}; do
  cat > doc-$i.md << EOF
# Document $i

## Section 1
Content for document $i, section 1.

## Section 2
Content for document $i, section 2.
EOF

  echo "Uploading document $i..."
  curl -s -X POST "http://localhost:5000/documents/upload" -F "file=@doc-$i.md" | jq -r '.documentId'
done

echo ""
echo "Waiting 30 seconds for indexing..."
sleep 30

echo ""
echo "Final status:"
curl -s "http://localhost:5000/documents" | jq '.[] | {id: .documentId, status: .status, chunks: .chunksCount}'

echo ""
echo "Qdrant total points:"
curl -s "http://localhost:6333/collections/technical_docs/points/count" | jq

# Cleanup
rm -f doc-*.md

echo ""
echo "✅ Load test complete"
```

### Test tempo indicizzazione

```bash
#!/bin/bash
# timing-test.sh

cat > timing-test.md << 'EOF'
# Timing Test
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
EOF

echo "⏱️  Starting timing test..."

start=$(date +%s)
response=$(curl -s -X POST "http://localhost:5000/documents/upload" -F "file=@timing-test.md")
doc_id=$(echo "$response" | jq -r '.documentId')
upload_time=$(date +%s)

echo "Upload time: $((upload_time - start)) seconds"

# Poll until indexed
while true; do
  status=$(curl -s "http://localhost:5000/documents/$doc_id" | jq -r '.status')
  if [ "$status" == "Indexed" ]; then
    break
  fi
  sleep 1
done

indexed_time=$(date +%s)
echo "Indexing time: $((indexed_time - upload_time)) seconds"
echo "Total time: $((indexed_time - start)) seconds"

# Cleanup
curl -s -X DELETE "http://localhost:5000/documents/$doc_id" > /dev/null
rm -f timing-test.md

echo "✅ Timing test complete"
```

---

## 🎯 Checklist test finale

Prima di considerare il sistema pronto:

- [ ] Tutti i servizi sono UP e healthy
- [ ] Upload documento funziona
- [ ] Indicizzazione automatica completa entro 15 secondi
- [ ] Chunk salvati correttamente in Qdrant con payload completo
- [ ] Duplicati rilevati correttamente (stesso SHA256)
- [ ] Eliminazione rimuove file + Qdrant + marca Deleted
- [ ] Re-indicizzazione funziona
- [ ] Validazione file respinge non-.md, vuoti, troppo grandi
- [ ] Log API mostrano operazioni corrette
- [ ] Swagger UI accessibile e funzionante
- [ ] Qdrant dashboard mostra collection con punti
- [ ] Backup/restore funzionano

---

## 📊 Metriche da monitorare

### Performance

- **Upload time**: < 1 secondo
- **Indexing time**: 5-15 secondi (dipende da dimensione file)
- **Chunk generation**: ~100 chunk/secondo
- **Embedding generation**: 2-5 secondi per chunk (dipende da CPU)
- **Qdrant upsert**: < 1 secondo per batch

### Risorse

```bash
# Monitoraggio in tempo reale
docker stats

# Output atteso:
# CONTAINER           CPU %   MEM USAGE / LIMIT     NET I/O
# technicaldocsqa-api 5%      200MB / 4GB          1MB / 500KB
# ollama              30%     1.5GB / 8GB          500KB / 2MB
# qdrant              2%      150MB / 4GB          100KB / 100KB
```

---

Per ulteriori test e debugging, consulta [DOCKER.md](DOCKER.md) per comandi avanzati di troubleshooting.
