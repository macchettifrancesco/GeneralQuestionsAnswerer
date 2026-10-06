# 🐳 Documentazione Docker - TechnicalDocsQa.Api

Guida completa all'utilizzo di Docker per il sistema RAG Pipeline 1.

## 📋 Indice

- [Architettura](#architettura)
- [Avvio rapido](#avvio-rapido)
- [Comandi disponibili](#comandi-disponibili)
- [Volumi persistenti](#volumi-persistenti)
- [Networking](#networking)
- [Sviluppo con hot-reload](#sviluppo-con-hot-reload)
- [Troubleshooting](#troubleshooting)
- [Best practices](#best-practices)

---

## 🏗️ Architettura

Il sistema è composto da 3 container Docker:

```
┌─────────────────────────────────────────────┐
│        Docker Network: rag-network          │
├─────────────────────────────────────────────┤
│                                             │
│  ┌──────────────┐    ┌──────────────┐     │
│  │   Ollama     │    │   Qdrant     │     │
│  │   :11434     │    │   :6333      │     │
│  │   nomic-     │    │   technical_ │     │
│  │   embed-text │    │   docs       │     │
│  └──────┬───────┘    └──────┬───────┘     │
│         │                    │             │
│         └──────┬─────────────┘             │
│                │                           │
│     ┌──────────▼──────────┐                │
│     │ TechnicalDocsQa.Api │                │
│     │       :5000         │                │
│     │   /swagger          │                │
│     └─────────────────────┘                │
│                                             │
└─────────────────────────────────────────────┘
```

### Container

| Container | Immagine | Porta | Descrizione |
|-----------|----------|-------|-------------|
| **api** | .NET 8 custom | 5000:8080 | API REST per upload e gestione documenti |
| **ollama** | ollama/ollama:latest | 11434:11434 | Servizio embedding con modello nomic-embed-text |
| **qdrant** | qdrant/qdrant:latest | 6333:6333, 6334:6334 | Vector database per chunk indicizzati |

---

## 🚀 Avvio rapido

### Metodo 1: Script helper (consigliato)

```bash
# Avvia tutto
./docker-helper.sh start

# Verifica stato
./docker-helper.sh status

# Vedi log
./docker-helper.sh logs-api
```

### Metodo 2: Docker Compose

```bash
# Avvia in background
docker-compose up -d

# Avvia con log in foreground
docker-compose up

# Verifica stato
docker-compose ps
```

### Metodo 3: Makefile

```bash
# Avvia tutto
make start

# Verifica stato
make status

# Test upload
make test-upload
```

---

## 🎯 Comandi disponibili

### Script helper (./docker-helper.sh)

```bash
start           # Avvia tutti i servizi
stop            # Arresta tutti i servizi
restart         # Riavvia tutti i servizi
logs            # Mostra log di tutti i servizi
logs-api        # Mostra log dell'API
logs-ollama     # Mostra log di Ollama
logs-qdrant     # Mostra log di Qdrant
status          # Mostra stato dei servizi
rebuild         # Ricompila e riavvia l'API
clean           # Arresta e rimuove tutto (DISTRUTTIVO)
backup          # Backup dei volumi
restore         # Ripristina backup
shell-api       # Apri shell nel container API
shell-ollama    # Apri shell nel container Ollama
test-upload     # Test upload documento
check-ollama    # Verifica modelli Ollama
check-qdrant    # Verifica collection Qdrant
help            # Mostra tutti i comandi
```

### Makefile

```bash
make start       # Avvia servizi
make stop        # Arresta servizi
make restart     # Riavvia servizi
make logs        # Mostra log
make status      # Verifica stato
make rebuild     # Rebuild API
make clean       # Pulizia completa (DISTRUTTIVO)
make backup      # Backup volumi
make test-upload # Test upload
make check       # Verifica sistema
make dev         # Avvia ambiente sviluppo
make help        # Mostra tutti i comandi
```

### Docker Compose raw

```bash
# Gestione servizi
docker-compose up -d                 # Avvia in background
docker-compose down                  # Arresta
docker-compose restart               # Riavvia
docker-compose ps                    # Stato
docker-compose logs -f               # Log live
docker-compose logs -f api           # Log API

# Build e rebuild
docker-compose build api             # Build API
docker-compose build --no-cache api  # Build senza cache
docker-compose up -d --build api     # Build e avvia

# Pulizia
docker-compose down -v               # Rimuovi volumi (DATI PERSI!)
docker-compose down --remove-orphans # Rimuovi container orfani
```

---

## 💾 Volumi persistenti

Docker utilizza volumi named per persistere i dati:

| Volume | Contenuto | Dimensione tipica |
|--------|-----------|-------------------|
| `qdrant_storage` | Database vettoriale Qdrant | 100MB - 10GB+ |
| `ollama_data` | Modelli Ollama scaricati | ~270MB per nomic-embed-text |
| `api_storage` | Documenti .md + registry JSON | Variabile (max 10MB/file) |

### Visualizzare volumi

```bash
docker volume ls | grep nsaquestionanswerer
```

### Ispezionare un volume

```bash
docker volume inspect nsaquestionanswerer_api_storage
```

### Backup manuale

```bash
# Crea directory backup
mkdir -p backups

# Backup Qdrant
docker run --rm \
  -v nsaquestionanswerer_qdrant_storage:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/qdrant-$(date +%Y%m%d_%H%M%S).tar.gz -C /data .

# Backup API storage
docker run --rm \
  -v nsaquestionanswerer_api_storage:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/api-storage-$(date +%Y%m%d_%H%M%S).tar.gz -C /data .
```

### Restore manuale

```bash
# Ferma i servizi prima!
docker-compose down

# Restore Qdrant (sostituisci TIMESTAMP)
docker run --rm \
  -v nsaquestionanswerer_qdrant_storage:/data \
  -v $(pwd)/backups:/backup \
  alpine tar xzf /backup/qdrant-TIMESTAMP.tar.gz -C /data

# Restore API storage
docker run --rm \
  -v nsaquestionanswerer_api_storage:/data \
  -v $(pwd)/backups:/backup \
  alpine tar xzf /backup/api-storage-TIMESTAMP.tar.gz -C /data

# Riavvia i servizi
docker-compose up -d
```

### Eliminare volumi (ATTENZIONE)

```bash
# Elimina volumi non utilizzati
docker volume prune

# Elimina specifico volume (DATI PERSI!)
docker volume rm nsaquestionanswerer_api_storage
```

---

## 🔗 Networking

### Rete Docker interna

I container comunicano tramite la rete `rag-network`:

```yaml
networks:
  rag-network:
    driver: bridge
```

### Risoluzione DNS interna

I container si trovano con i nomi dei servizi:

- API → Ollama: `http://ollama:11434`
- API → Qdrant: `http://qdrant:6333`

### Port mapping

| Servizio | Interno | Esterno (host) |
|----------|---------|----------------|
| API | 8080 | 5000 |
| Ollama | 11434 | 11434 |
| Qdrant HTTP | 6333 | 6333 |
| Qdrant gRPC | 6334 | 6334 |

### Personalizzare porte

Modifica `docker-compose.yml`:

```yaml
api:
  ports:
    - "8000:8080"  # Esponi API su porta 8000 invece di 5000
```

---

## 🔨 Sviluppo con hot-reload

Per sviluppo con ricaricamento automatico del codice:

```bash
# Usa docker-compose.dev.yml
docker-compose -f docker-compose.dev.yml up

# Oppure con Makefile
make dev
```

Il codice sorgente viene montato come volume, quindi le modifiche sono riflesse immediatamente senza rebuild.

### Differenze dev vs prod

| Caratteristica | Produzione | Sviluppo |
|----------------|------------|----------|
| Build | Multi-stage ottimizzato | SDK completo |
| Volumi | Named volumes | Named volumes + source code mount |
| Hot-reload | ❌ No | ✅ Sì (dotnet watch) |
| Dimensione immagine | ~200MB | ~700MB |

### Workflow sviluppo

1. Avvia ambiente dev:
   ```bash
   make dev
   ```

2. Modifica codice in `TechnicalDocsQa.Api/`

3. Il container rileva le modifiche e ricompila automaticamente

4. Testa su http://localhost:5000/swagger

5. Commit delle modifiche

6. Rebuild produzione:
   ```bash
   make rebuild
   ```

---

## 🐛 Troubleshooting

### Problema: Ollama non scarica il modello

**Sintomo**: L'API non riesce a generare embedding, log Ollama mostra errori.

**Soluzione**:
```bash
# Verifica log Ollama
docker-compose logs ollama

# Scarica manualmente il modello
docker exec -it ollama ollama pull nomic-embed-text

# Verifica modelli installati
docker exec -it ollama ollama list
```

---

### Problema: Qdrant non crea la collection

**Sintomo**: API restituisce errori 404 durante upsert chunk.

**Soluzione**:
```bash
# Verifica che Qdrant sia healthy
docker-compose ps

# Controlla log Qdrant
docker-compose logs qdrant

# Verifica manualmente la collection
curl http://localhost:6333/collections/technical_docs

# Crea manualmente se necessario
curl -X PUT http://localhost:6333/collections/technical_docs \
  -H "Content-Type: application/json" \
  -d '{
    "vectors": {
      "size": 768,
      "distance": "Cosine"
    }
  }'
```

---

### Problema: API non si connette a Ollama/Qdrant

**Sintomo**: Timeout o connection refused nei log API.

**Cause possibili**:
- Servizi non ancora pronti (health check in corso)
- Problema networking Docker
- Container in crash loop

**Soluzione**:
```bash
# Verifica health check
docker-compose ps

# Se non healthy, controlla i log
docker-compose logs ollama
docker-compose logs qdrant

# Riavvia i servizi problematici
docker-compose restart ollama qdrant

# In caso di problemi di rete, ricrea i container
docker-compose down
docker-compose up -d
```

---

### Problema: Porte già in uso

**Sintomo**: `Error starting userland proxy: listen tcp4 0.0.0.0:5000: bind: address already in use`

**Soluzione**:
```bash
# Trova il processo che usa la porta 5000
lsof -i :5000
# oppure
netstat -tuln | grep 5000

# Termina il processo
kill <PID>

# Oppure cambia porta in docker-compose.yml
ports:
  - "5555:8080"  # Usa 5555 invece di 5000
```

---

### Problema: Spazio disco insufficiente

**Sintomo**: `no space left on device` durante build o pull.

**Soluzione**:
```bash
# Verifica spazio Docker
docker system df

# Pulizia immagini non usate
docker image prune -a

# Pulizia volumi non usati
docker volume prune

# Pulizia cache build
docker builder prune

# Pulizia completa (ATTENZIONE: rimuove tutto)
docker system prune -a --volumes
```

---

### Problema: Container in crash loop

**Sintomo**: Container si riavvia continuamente.

**Soluzione**:
```bash
# Vedi log ultimi 100 righe
docker-compose logs --tail=100 api

# Vedi eventi Docker
docker events --filter container=technicaldocsqa-api

# Ispeziona container
docker inspect technicaldocsqa-api

# Avvia in modalità interattiva per debug
docker-compose run --rm api /bin/bash
```

---

## ✅ Best practices

### Produzione

1. **Usa volumi named** invece di bind mounts per performance migliori
2. **Configura limiti risorse**:
   ```yaml
   deploy:
     resources:
       limits:
         cpus: '2'
         memory: 4G
   ```
3. **Abilita health checks** personalizzati
4. **Usa secrets** per credenziali:
   ```yaml
   secrets:
     api_key:
       file: ./secrets/api_key.txt
   ```
5. **Monitora con Prometheus/Grafana**
6. **Configura log rotation**:
   ```yaml
   logging:
     driver: "json-file"
     options:
       max-size: "10m"
       max-file: "3"
   ```

### Sviluppo

1. **Usa `docker-compose.dev.yml`** per hot-reload
2. **Monta `.env`** per variabili d'ambiente locali
3. **Non committare volumi** nel repo (`.gitignore`)
4. **Backup regolari** prima di modifiche strutturali
5. **Testa in container** prima di committare

### Sicurezza

1. **Non esporre porte non necessarie**
2. **Usa network interni** per comunicazione inter-container
3. **Scansiona immagini** con `docker scan`:
   ```bash
   docker scan technicaldocsqa-api
   ```
4. **Aggiorna immagini base** regolarmente
5. **Non eseguire come root** nel container (già configurato con user aspnet)

---

## 📊 Monitoraggio

### Metriche Docker

```bash
# Statistiche container in tempo reale
docker stats

# Con formato personalizzato
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"
```

### Metriche Qdrant

```bash
# Info collection
curl http://localhost:6333/collections/technical_docs | jq

# Conteggio punti
curl http://localhost:6333/collections/technical_docs/points/count | jq
```

### Health checks

```bash
# Stato health
docker inspect --format='{{json .State.Health}}' ollama | jq
docker inspect --format='{{json .State.Health}}' qdrant | jq
```

---

## 🔄 Aggiornamenti

### Aggiornare immagini

```bash
# Pull nuove versioni
docker-compose pull

# Riavvia con nuove immagini
docker-compose up -d

# Verifica versioni
docker-compose images
```

### Aggiornare il codice API

```bash
# Modifica codice in TechnicalDocsQa.Api/

# Rebuild
docker-compose build api

# Riavvia
docker-compose up -d api
```

---

## 📚 Riferimenti

- [Docker Compose reference](https://docs.docker.com/compose/compose-file/)
- [Ollama Docker](https://hub.docker.com/r/ollama/ollama)
- [Qdrant Docker](https://hub.docker.com/r/qdrant/qdrant)
- [.NET Docker](https://hub.docker.com/_/microsoft-dotnet)

---

Per ulteriori informazioni, consulta [README.md](README.md) e [QUICKSTART.md](QUICKSTART.md).
