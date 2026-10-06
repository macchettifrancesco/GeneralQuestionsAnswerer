# TechnicalDocsQa - Complete RAG System

Sistema completo di RAG (Retrieval-Augmented Generation) per documentazione tecnica con indicizzazione automatica e Q&A intelligente, containerizzato con Docker.

## 🎯 Panoramica

**TechnicalDocsQa** è composto da due pipeline:

- **Pipeline 1**: Sistema di indicizzazione automatica per documenti Markdown con embedding Ollama e vector store Qdrant
- **Pipeline 2**: Sistema di Q&A intelligente (RAG) con interfaccia Blazor WebAssembly

## 🚀 Avvio Rapido Completo

```bash
# Avvia tutti i servizi (API + Blazor UI + Ollama + Qdrant)
docker-compose up -d

# Visualizza i log
docker-compose logs -f

# Visualizza lo stato dei servizi
docker-compose ps
```

**Nota**: Il primo avvio richiede 5-10 minuti perché Ollama deve scaricare i modelli:
- `nomic-embed-text` (~270MB) - per embeddings
- `llama3.1` (~4.7GB) - per chat completions

### Servizi Disponibili

| Servizio | URL | Descrizione |
|----------|-----|-------------|
| **Blazor UI** | http://localhost:5173 | Interfaccia web per upload e Q&A |
| **API** | http://localhost:5000 | REST API |
| **Swagger** | http://localhost:5000/swagger | Documentazione API interattiva |
| **Qdrant** | http://localhost:6333 | Vector database |
| **Qdrant Dashboard** | http://localhost:6333/dashboard | UI Qdrant |
| **Ollama** | http://localhost:11434 | Servizio AI (embedding + chat) |

## 📖 Documentazione

- **[PIPELINE2.md](./PIPELINE2.md)**: Documentazione completa Pipeline 2 (Q&A + Blazor UI)
- **[QUICKSTART.md](./QUICKSTART.md)**: Guida rapida
- **[TESTING.md](./TESTING.md)**: Guida ai test
- **[DOCKER.md](./DOCKER.md)**: Documentazione Docker

## 🎨 Utilizzo con l'Interfaccia Web

1. Apri http://localhost:5173
2. Vai su **Upload** e carica un file Markdown
3. Attendi l'indicizzazione automatica (controlla su **Documents**)
4. Vai su **Ask Question** e fai una domanda
5. Ricevi una risposta con citazioni delle fonti!

---

# TechnicalDocsQa.Api - RAG Pipeline 1

Sistema di indicizzazione automatica per documenti Markdown con embedding Ollama e vector store Qdrant, completamente containerizzato con Docker.

## 🐳 Avvio rapido con Docker

### Prerequisiti

- Docker Desktop installato e in esecuzione
- Docker Compose v2+

### Avvio completo del sistema

```bash
# Avvia tutti i servizi (API + Ollama + Qdrant)
docker-compose up -d

# Visualizza i log
docker-compose logs -f

# Visualizza lo stato dei servizi
docker-compose ps
```

**Nota**: Il primo avvio richiede alcuni minuti perché Ollama deve scaricare il modello `nomic-embed-text` (~270MB).

### Servizi disponibili

| Servizio | URL | Descrizione |
|----------|-----|-------------|
| **API** | http://localhost:5000 | TechnicalDocsQa.Api REST API |
| **Swagger** | http://localhost:5000/swagger | Documentazione API interattiva |
| **Qdrant** | http://localhost:6333 | Vector database |
| **Qdrant Dashboard** | http://localhost:6333/dashboard | UI Qdrant |
| **Ollama** | http://localhost:11434 | Servizio embedding |

---

## 📡 Test degli endpoint

### 1. Upload di un documento

```bash
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@test.md"
```

Risposta:
```json
{
  "documentId": "b7e1f5d4-5a20-4c0d-8f8b-37ef01f62a11",
  "fileName": "test.md",
  "status": "Uploaded",
  "message": "Document uploaded. Indexing has been queued."
}
```

### 2. Lista documenti

```bash
curl "http://localhost:5000/documents"
```

### 3. Dettaglio documento

```bash
curl "http://localhost:5000/documents/{documentId}"
```

### 4. Verifica indicizzazione in Qdrant

Apri il browser: http://localhost:6333/dashboard

Verifica la collection `technical_docs` con i chunk indicizzati.

---

## 🛠️ Comandi Docker

### Avvio e arresto

```bash
# Avvia in background
docker-compose up -d

# Avvia con log in foreground
docker-compose up

# Arresta i servizi
docker-compose down

# Arresta e rimuovi volumi (ATTENZIONE: elimina tutti i dati)
docker-compose down -v

# Riavvia un singolo servizio
docker-compose restart api
```

### Logs e debugging

```bash
# Log di tutti i servizi
docker-compose logs -f

# Log di un servizio specifico
docker-compose logs -f api
docker-compose logs -f ollama
docker-compose logs -f qdrant

# Log ultimi 100 righe
docker-compose logs --tail=100 api
```

### Rebuild dell'API

```bash
# Rebuild dopo modifiche al codice
docker-compose build api

# Rebuild senza cache
docker-compose build --no-cache api

# Rebuild e restart
docker-compose up -d --build api
```

### Accesso ai container

```bash
# Apri una shell nel container API
docker exec -it technicaldocsqa-api /bin/bash

# Apri una shell in Ollama
docker exec -it ollama /bin/bash

# Verifica modelli Ollama installati
docker exec -it ollama ollama list
```

---

## 🗂️ Volumi persistenti

I dati sono salvati in volumi Docker persistenti:

- `qdrant_storage` - Database vettoriale Qdrant
- `ollama_data` - Modelli Ollama scaricati
- `api_storage` - Documenti caricati e registry JSON

### Backup dei dati

```bash
# Backup del volume Qdrant
docker run --rm -v qdrant_storage:/data -v $(pwd):/backup alpine tar czf /backup/qdrant-backup.tar.gz -C /data .

# Backup del volume API storage
docker run --rm -v api_storage:/data -v $(pwd):/backup alpine tar czf /backup/api-storage-backup.tar.gz -C /data .
```

### Restore dei dati

```bash
# Restore del volume Qdrant
docker run --rm -v qdrant_storage:/data -v $(pwd):/backup alpine tar xzf /backup/qdrant-backup.tar.gz -C /data

# Restore del volume API storage
docker run --rm -v api_storage:/data -v $(pwd):/backup alpine tar xzf /backup/api-storage-backup.tar.gz -C /data
```

---

## 🔧 Configurazione

La configurazione è gestita tramite variabili d'ambiente in `docker-compose.yml`:

```yaml
environment:
  - Storage__DocumentsPath=/app/Storage/documents
  - Storage__RegistryPath=/app/Storage/registry/documents.json
  - Ollama__BaseUrl=http://ollama:11434
  - Qdrant__BaseUrl=http://qdrant:6333
```

Modifica `docker-compose.yml` per personalizzare:
- Porte esposte
- Dimensione massima file upload
- Parametri di chunking
- Modello Ollama (default: `nomic-embed-text`)

---

## 📊 Monitoraggio

### Verifica salute dei servizi

```bash
# Stato health check
docker-compose ps

# Dettagli health check
docker inspect --format='{{json .State.Health}}' ollama | jq
docker inspect --format='{{json .State.Health}}' qdrant | jq
```

### Metriche Qdrant

```bash
# Informazioni sulla collection
curl http://localhost:6333/collections/technical_docs

# Conteggio punti
curl http://localhost:6333/collections/technical_docs/points/count
```

---

## 🐛 Troubleshooting

### Problema: Ollama non scarica il modello

```bash
# Verifica i log
docker-compose logs ollama

# Scarica manualmente il modello
docker exec -it ollama ollama pull nomic-embed-text

# Verifica modelli installati
docker exec -it ollama ollama list
```

### Problema: API non si connette a Ollama/Qdrant

Verifica che i servizi siano healthy:
```bash
docker-compose ps
```

Se non sono healthy, controlla i log:
```bash
docker-compose logs qdrant
docker-compose logs ollama
```

### Problema: Port già in uso

Modifica le porte in `docker-compose.yml`:
```yaml
ports:
  - "5555:8080"  # Cambia 5000 in 5555
```

### Problema: Spazio disco insufficiente

```bash
# Rimuovi volumi non utilizzati
docker volume prune

# Rimuovi immagini non utilizzate
docker image prune -a
```

---

## 🚀 Sviluppo

### Sviluppo con hot-reload

Per sviluppo con ricaricamento automatico:

```bash
# Monta il codice sorgente come volume
docker-compose -f docker-compose.dev.yml up
```

### Test locali senza Docker

Se preferisci sviluppare senza Docker:

1. Avvia solo i servizi esterni:
```bash
docker-compose up -d ollama qdrant
```

2. Avvia l'API localmente:
```bash
cd TechnicalDocsQa.Api
dotnet run
```

3. Configura `appsettings.Development.json`:
```json
{
  "Ollama": { "BaseUrl": "http://localhost:11434" },
  "Qdrant": { "BaseUrl": "http://localhost:6333" }
}
```

---

## 📦 Architettura Docker

```
┌─────────────────────────────────────────────────┐
│           Docker Network: rag-network           │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌───────────────┐      ┌───────────────┐     │
│  │   Ollama      │      │    Qdrant     │     │
│  │  Port: 11434  │      │  Port: 6333   │     │
│  │  Model: nomic │      │  Collection:  │     │
│  │  -embed-text  │      │  technical_   │     │
│  │               │      │  docs         │     │
│  └───────┬───────┘      └───────┬───────┘     │
│          │                      │             │
│          └──────────┬───────────┘             │
│                     │                         │
│          ┌──────────▼───────────┐             │
│          │  TechnicalDocsQa.Api │             │
│          │    Port: 5000        │             │
│          │    Swagger: /swagger │             │
│          └──────────────────────┘             │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 📝 Note

- **Volumi persistenti**: I dati sopravvivono ai restart dei container
- **Health checks**: L'API attende che Ollama e Qdrant siano pronti prima di avviarsi
- **Network isolation**: Tutti i servizi comunicano su una rete Docker dedicata
- **Primo avvio**: Richiede ~5 minuti per scaricare il modello Ollama
- **HTTPS**: Disabilitato di default. Per abilitarlo, genera certificati e monta in `/https`

---

## 📚 Endpoint disponibili

| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| POST | `/documents/upload` | Carica un file .md e lo accoda per indicizzazione |
| GET | `/documents` | Lista di tutti i documenti caricati |
| GET | `/documents/{id}` | Dettaglio di un documento specifico |
| DELETE | `/documents/{id}` | Elimina documento, file e chunk Qdrant |
| POST | `/documents/{id}/reindex` | Riaccoda documento per re-indicizzazione |

---

## 🎯 Stati documento

- `Uploaded` - File ricevuto e salvato
- `Indexing` - Indicizzazione in corso
- `Indexed` - Embedding salvati in Qdrant
- `Failed` - Errore durante indicizzazione
- `Deleted` - Documento eliminato

---

## 🔐 Sicurezza

Per produzione:
- Abilita HTTPS con certificati validi
- Aggiungi autenticazione/autorizzazione
- Limita le porte esposte
- Usa Docker secrets per credenziali sensibili
- Configura rate limiting

---

## 📄 Licenza

Progetto didattico - Pipeline 1 RAG per indicizzazione documenti Markdown.
