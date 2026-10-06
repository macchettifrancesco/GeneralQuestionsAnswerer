---
title: Guida configurazione sistema RAG
author: Technical Team
date: 2026-05-21
tags: [rag, embeddings, qdrant, ollama]
---

# Guida alla configurazione del sistema RAG Pipeline 1

## Introduzione

Questo documento descrive la configurazione e l'utilizzo del sistema RAG (Retrieval-Augmented Generation) Pipeline 1 per l'indicizzazione automatica di documenti Markdown.

## Architettura del sistema

Il sistema è composto da tre componenti principali:

### 1. API REST (TechnicalDocsQa.Api)

L'API REST fornisce gli endpoint per:
- Upload di documenti Markdown
- Gestione del ciclo di vita dei documenti
- Monitoraggio dello stato di indicizzazione
- Eliminazione e re-indicizzazione

### 2. Ollama Embedding Service

Ollama genera gli embedding vettoriali utilizzando il modello `nomic-embed-text`. Caratteristiche principali:

- Dimensione vettori: 768 dimensioni
- Modello open-source e locale
- Nessun dato inviato a servizi esterni
- Performance ottimizzate per CPU

### 3. Qdrant Vector Database

Qdrant memorizza i chunk di testo indicizzati con i relativi embedding. Funzionalità:

- Ricerca per similarità vettoriale
- Filtri su metadata (documentId, sezione, titolo)
- Storage persistente
- API REST per interrogazioni

## Processo di indicizzazione

Il flusso di indicizzazione automatica avviene in questi passaggi:

1. **Upload**: L'utente carica un file .md tramite API
2. **Validazione**: Controllo estensione, dimensione, formato
3. **Storage**: Salvataggio del file su disco
4. **Accodamento**: Il documentId viene inserito in una coda asincrona
5. **Background Processing**: Un worker preleva il job dalla coda
6. **Parsing**: Rimozione frontmatter YAML ed estrazione sezioni
7. **Chunking**: Divisione del testo in chunk semantici
8. **Embedding**: Generazione vettori per ogni chunk tramite Ollama
9. **Upsert**: Salvataggio chunk e embeddings in Qdrant
10. **Completamento**: Aggiornamento stato documento a "Indexed"

## Configurazione

### Storage

```json
{
  "Storage": {
    "DocumentsPath": "Storage/documents",
    "RegistryPath": "Storage/registry/documents.json",
    "AllowedExtensions": [".md"],
    "MaxFileSizeMb": 10
  }
}
```

### Chunking

La strategia di chunking preferisce le sezioni Markdown quando disponibili:

```json
{
  "Chunking": {
    "MaxChunkCharacters": 3000,
    "OverlapCharacters": 400
  }
}
```

Per documenti senza sezioni, viene utilizzato un chunking a finestra scorrevole con overlap.

### Ollama

Configurazione del servizio di embedding:

```json
{
  "Ollama": {
    "BaseUrl": "http://ollama:11434",
    "EmbeddingModel": "nomic-embed-text"
  }
}
```

### Qdrant

Configurazione del vector database:

```json
{
  "Qdrant": {
    "BaseUrl": "http://qdrant:6333",
    "CollectionName": "technical_docs",
    "VectorSize": 768,
    "Distance": "Cosine"
  }
}
```

## Deployment con Docker

### Avvio rapido

```bash
docker-compose up -d
```

### Verifica stato

```bash
docker-compose ps
```

### Visualizza logs

```bash
docker-compose logs -f api
```

## Stati del documento

Ogni documento può trovarsi in uno dei seguenti stati:

- **Uploaded**: File ricevuto e salvato, in attesa di indicizzazione
- **Indexing**: Indicizzazione in corso
- **Indexed**: Indicizzazione completata con successo
- **Failed**: Errore durante l'indicizzazione (controllare ErrorMessage)
- **Deleted**: Documento eliminato logicamente

## Troubleshooting

### Documento bloccato in stato "Indexing"

Possibili cause:
- Ollama non disponibile o modello non scaricato
- Qdrant non raggiungibile
- Errore di parsing del Markdown

Soluzione: Controllare i log del servizio API e dei servizi esterni.

### Performance lente

Se l'indicizzazione è lenta:
- Verificare risorse CPU/memoria disponibili
- Ridurre MaxChunkCharacters per generare meno chunk
- Verificare latenza rete tra API e Ollama/Qdrant

### Duplicati non rilevati

Il sistema rileva duplicati tramite hash SHA256 del contenuto. Se modifichi anche un singolo carattere, verrà considerato un nuovo documento.

## Best Practices

### Organizzazione documenti

- Usa titoli e sezioni Markdown chiare (`#`, `##`, `###`)
- Mantieni sezioni di lunghezza ragionevole (< 3000 caratteri)
- Rimuovi immagini e contenuti binari prima dell'upload

### Metadata

Includi frontmatter YAML per metadata strutturati:

```yaml
---
title: Titolo documento
author: Nome Autore
date: 2026-05-21
tags: [tag1, tag2]
---
```

Nota: Il frontmatter viene rimosso durante il parsing ma i metadata possono essere estesi in future versioni.

### Manutenzione

- Backup regolare dei volumi Docker (Qdrant e API storage)
- Monitoraggio dello spazio disco
- Pulizia periodica di documenti obsoleti

## Limitazioni note

- Supporto solo per file Markdown (.md)
- Dimensione massima file configurabile (default 10MB)
- Un solo modello di embedding configurabile
- Registry JSON (migrazione a SQLite consigliata per produzione)

## Conclusione

Questo sistema fornisce una pipeline robusta e scalabile per l'indicizzazione automatica di documenti tecnici, completamente containerizzata e pronta per l'uso in produzione dopo opportune configurazioni di sicurezza.

Per maggiori informazioni, consultare la documentazione API su http://localhost:5000/swagger
