# 🚀 Quick Start - TechnicalDocsQa.Api

Inizia a usare il sistema RAG in 3 minuti!

## Prerequisiti

- Docker Desktop installato ✅
- 5 minuti di tempo ⏱️

## Passo 1: Avvia il sistema

```bash
cd NSAQuestionAnswerer
./docker-helper.sh start
```

Oppure:
```bash
docker-compose up -d
```

**⏳ Primo avvio**: Attendi ~5 minuti per il download del modello Ollama (270MB)

## Passo 2: Verifica che tutto sia pronto

```bash
./docker-helper.sh status
```

Dovresti vedere tutti i servizi "healthy":
```
NAME                    STATUS         PORTS
technicaldocsqa-api     Up (healthy)   0.0.0.0:5000->8080/tcp
ollama                  Up (healthy)   0.0.0.0:11434->11434/tcp
qdrant                  Up (healthy)   0.0.0.0:6333->6333/tcp
```

## Passo 3: Carica il primo documento

```bash
./docker-helper.sh test-upload
```

Oppure manualmente:
```bash
curl -X POST "http://localhost:5000/documents/upload" \
  -F "file=@example-document.md"
```

## Passo 4: Verifica l'indicizzazione

Apri il browser: **http://localhost:5000/swagger**

Oppure:
```bash
curl http://localhost:5000/documents
```

Dopo ~10 secondi vedrai lo stato cambiare da `Uploaded` → `Indexed`

## Passo 5: Controlla Qdrant

Apri: **http://localhost:6333/dashboard**

Verifica la collection `technical_docs` con i chunk indicizzati.

---

## ✅ Fatto!

Il tuo sistema RAG è pronto. I documenti vengono automaticamente:
- ✅ Salvati su disco
- ✅ Parsati e divisi in chunk
- ✅ Trasformati in embedding vettoriali
- ✅ Indicizzati in Qdrant

---

## 📚 Comandi utili

```bash
# Vedi i log in tempo reale
./docker-helper.sh logs-api

# Ferma tutto
./docker-helper.sh stop

# Riavvia dopo modifiche al codice
./docker-helper.sh rebuild

# Backup dei dati
./docker-helper.sh backup

# Verifica modelli Ollama
./docker-helper.sh check-ollama

# Verifica Qdrant
./docker-helper.sh check-qdrant

# Help completo
./docker-helper.sh help
```

---

## 🐛 Problemi?

### Ollama non scarica il modello
```bash
docker exec -it ollama ollama pull nomic-embed-text
```

### Porte già in uso
Modifica le porte in `docker-compose.yml`:
```yaml
ports:
  - "5555:8080"  # Cambia 5000 in 5555
```

### API non risponde
```bash
./docker-helper.sh logs-api
```

---

## 📖 Documentazione completa

Leggi [README.md](README.md) per tutti i dettagli.

---

## 🎯 Prossimi passi

Una volta che il sistema funziona, puoi:

1. **Carica i tuoi documenti**
   ```bash
   curl -X POST "http://localhost:5000/documents/upload" -F "file=@tuo-documento.md"
   ```

2. **Integra con il tuo frontend**
   - Usa gli endpoint REST documentati in Swagger
   - Monitora lo stato dei documenti
   - Gestisci la re-indicizzazione

3. **Prepara per produzione**
   - Aggiungi autenticazione
   - Configura HTTPS
   - Implementa rate limiting
   - Migra registry da JSON a database

4. **Costruisci la Pipeline 2**
   - Question answering con LLM
   - Ricerca semantica in Qdrant
   - Generazione risposte con contesto

---

Buon lavoro! 🎉
