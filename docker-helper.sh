#!/bin/bash

# TechnicalDocsQa.Api - Docker Management Script
# Comandi helper per gestire il sistema RAG Pipeline 1

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

function print_help() {
    echo -e "${BLUE}TechnicalDocsQa.Api - Docker Management Script${NC}"
    echo ""
    echo "Usage: ./docker-helper.sh [comando]"
    echo ""
    echo "Comandi disponibili:"
    echo "  start           - Avvia tutti i servizi"
    echo "  stop            - Arresta tutti i servizi"
    echo "  restart         - Riavvia tutti i servizi"
    echo "  logs            - Mostra i log di tutti i servizi"
    echo "  logs-api        - Mostra i log dell'API"
    echo "  logs-ollama     - Mostra i log di Ollama"
    echo "  logs-qdrant     - Mostra i log di Qdrant"
    echo "  status          - Mostra lo stato dei servizi"
    echo "  rebuild         - Ricompila e riavvia l'API"
    echo "  clean           - Arresta e rimuove tutto (inclusi volumi)"
    echo "  backup          - Crea backup dei volumi"
    echo "  restore         - Ripristina backup dei volumi"
    echo "  shell-api       - Apri shell nel container API"
    echo "  shell-ollama    - Apri shell nel container Ollama"
    echo "  test-upload     - Test upload di un documento di esempio"
    echo "  check-ollama    - Verifica modelli Ollama installati"
    echo "  check-qdrant    - Verifica collection Qdrant"
    echo "  help            - Mostra questo messaggio"
    echo ""
}

function start_services() {
    echo -e "${GREEN}🚀 Avvio servizi...${NC}"
    docker-compose up -d
    echo ""
    echo -e "${GREEN}✅ Servizi avviati!${NC}"
    echo ""
    echo "Servizi disponibili:"
    echo "  API:              http://localhost:5000"
    echo "  Swagger UI:       http://localhost:5000/swagger"
    echo "  Qdrant:           http://localhost:6333"
    echo "  Qdrant Dashboard: http://localhost:6333/dashboard"
    echo "  Ollama:           http://localhost:11434"
    echo ""
    echo -e "${YELLOW}⏳ Primo avvio: attendere ~5 minuti per il download del modello Ollama${NC}"
}

function stop_services() {
    echo -e "${YELLOW}🛑 Arresto servizi...${NC}"
    docker-compose down
    echo -e "${GREEN}✅ Servizi arrestati${NC}"
}

function restart_services() {
    echo -e "${YELLOW}🔄 Riavvio servizi...${NC}"
    docker-compose restart
    echo -e "${GREEN}✅ Servizi riavviati${NC}"
}

function show_logs() {
    echo -e "${BLUE}📋 Visualizzazione log (Ctrl+C per uscire)${NC}"
    docker-compose logs -f
}

function show_logs_api() {
    echo -e "${BLUE}📋 Log API (Ctrl+C per uscire)${NC}"
    docker-compose logs -f api
}

function show_logs_ollama() {
    echo -e "${BLUE}📋 Log Ollama (Ctrl+C per uscire)${NC}"
    docker-compose logs -f ollama
}

function show_logs_qdrant() {
    echo -e "${BLUE}📋 Log Qdrant (Ctrl+C per uscire)${NC}"
    docker-compose logs -f qdrant
}

function show_status() {
    echo -e "${BLUE}📊 Stato servizi:${NC}"
    docker-compose ps
}

function rebuild_api() {
    echo -e "${YELLOW}🔨 Rebuild API...${NC}"
    docker-compose build --no-cache api
    docker-compose up -d api
    echo -e "${GREEN}✅ API ricompilata e riavviata${NC}"
}

function clean_all() {
    echo -e "${RED}⚠️  ATTENZIONE: Questa operazione rimuoverà TUTTI i dati (documenti, registry, collection Qdrant)${NC}"
    read -p "Sei sicuro? (yes/no): " confirm
    if [ "$confirm" == "yes" ]; then
        echo -e "${YELLOW}🧹 Pulizia completa...${NC}"
        docker-compose down -v
        echo -e "${GREEN}✅ Pulizia completata${NC}"
    else
        echo -e "${BLUE}Operazione annullata${NC}"
    fi
}

function backup_volumes() {
    echo -e "${BLUE}💾 Backup volumi...${NC}"
    mkdir -p backups
    timestamp=$(date +%Y%m%d_%H%M%S)
    
    echo "Backup Qdrant..."
    docker run --rm -v nsaquestionanswerer_qdrant_storage:/data -v $(pwd)/backups:/backup alpine tar czf /backup/qdrant-${timestamp}.tar.gz -C /data .
    
    echo "Backup API storage..."
    docker run --rm -v nsaquestionanswerer_api_storage:/data -v $(pwd)/backups:/backup alpine tar czf /backup/api-storage-${timestamp}.tar.gz -C /data .
    
    echo -e "${GREEN}✅ Backup completati in ./backups/${NC}"
    ls -lh backups/*${timestamp}*
}

function restore_volumes() {
    echo -e "${BLUE}📥 Ripristino volumi${NC}"
    echo "File di backup disponibili:"
    ls -1 backups/ 2>/dev/null || echo "Nessun backup trovato"
    echo ""
    read -p "Inserisci il timestamp del backup da ripristinare (es. 20260521_143000): " timestamp
    
    if [ -f "backups/qdrant-${timestamp}.tar.gz" ]; then
        echo "Ripristino Qdrant..."
        docker run --rm -v nsaquestionanswerer_qdrant_storage:/data -v $(pwd)/backups:/backup alpine tar xzf /backup/qdrant-${timestamp}.tar.gz -C /data
    fi
    
    if [ -f "backups/api-storage-${timestamp}.tar.gz" ]; then
        echo "Ripristino API storage..."
        docker run --rm -v nsaquestionanswerer_api_storage:/data -v $(pwd)/backups:/backup alpine tar xzf /backup/api-storage-${timestamp}.tar.gz -C /data
    fi
    
    echo -e "${GREEN}✅ Ripristino completato${NC}"
}

function shell_api() {
    echo -e "${BLUE}🐚 Apertura shell in API container${NC}"
    docker exec -it technicaldocsqa-api /bin/bash
}

function shell_ollama() {
    echo -e "${BLUE}🐚 Apertura shell in Ollama container${NC}"
    docker exec -it ollama /bin/bash
}

function test_upload() {
    echo -e "${BLUE}🧪 Test upload documento${NC}"
    
    # Crea un documento di test se non esiste
    if [ ! -f "test-document.md" ]; then
        cat > test-document.md << 'EOF'
# Test Document

## Introduction

This is a test document for the RAG Pipeline 1 indexing system.

## Features

- Markdown parsing
- YAML frontmatter removal
- Section-based chunking
- Ollama embeddings
- Qdrant vector storage

## Conclusion

This document will be automatically indexed and stored in Qdrant.
EOF
        echo -e "${GREEN}📄 Documento di test creato: test-document.md${NC}"
    fi
    
    echo "Uploading test-document.md..."
    response=$(curl -s -X POST "http://localhost:5000/documents/upload" -F "file=@test-document.md")
    echo ""
    echo -e "${GREEN}Risposta:${NC}"
    echo "$response" | jq . 2>/dev/null || echo "$response"
    echo ""
    
    documentId=$(echo "$response" | jq -r '.documentId' 2>/dev/null)
    if [ "$documentId" != "null" ] && [ -n "$documentId" ]; then
        echo -e "${BLUE}Verifica stato documento tra 10 secondi...${NC}"
        sleep 10
        curl -s "http://localhost:5000/documents/$documentId" | jq . 2>/dev/null || echo "Errore nel recupero del documento"
    fi
}

function check_ollama() {
    echo -e "${BLUE}🤖 Modelli Ollama installati:${NC}"
    docker exec ollama ollama list
}

function check_qdrant() {
    echo -e "${BLUE}🗄️  Verifica collection Qdrant:${NC}"
    echo ""
    echo "Collection info:"
    curl -s "http://localhost:6333/collections/technical_docs" | jq . 2>/dev/null || echo "Collection non ancora creata o Qdrant non disponibile"
    echo ""
    echo "Point count:"
    curl -s "http://localhost:6333/collections/technical_docs/points/count" | jq . 2>/dev/null || echo "Collection non ancora creata"
}

# Main script
case "${1:-help}" in
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    logs)
        show_logs
        ;;
    logs-api)
        show_logs_api
        ;;
    logs-ollama)
        show_logs_ollama
        ;;
    logs-qdrant)
        show_logs_qdrant
        ;;
    status)
        show_status
        ;;
    rebuild)
        rebuild_api
        ;;
    clean)
        clean_all
        ;;
    backup)
        backup_volumes
        ;;
    restore)
        restore_volumes
        ;;
    shell-api)
        shell_api
        ;;
    shell-ollama)
        shell_ollama
        ;;
    test-upload)
        test_upload
        ;;
    check-ollama)
        check_ollama
        ;;
    check-qdrant)
        check_qdrant
        ;;
    help|*)
        print_help
        ;;
esac
