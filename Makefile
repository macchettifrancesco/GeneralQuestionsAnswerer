.PHONY: help start stop restart logs status rebuild clean backup test-upload check dev

# Colors for output
GREEN  := \033[0;32m
YELLOW := \033[1;33m
BLUE   := \033[0;34m
NC     := \033[0m # No Color

help: ## Show this help message
	@echo "$(BLUE)TechnicalDocsQa - Complete RAG System$(NC)"
	@echo ""
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-15s$(NC) %s\n", $$1, $$2}'

start: ## Start all services (API + Blazor UI + Ollama + Qdrant)
	@echo "$(GREEN)🚀 Starting services...$(NC)"
	docker-compose up -d
	@echo "$(GREEN)✅ Services started!$(NC)"
	@echo ""
	@echo "Blazor UI:        http://localhost:5173"
	@echo "API:              http://localhost:5000"
	@echo "Swagger UI:       http://localhost:5000/swagger"
	@echo "Qdrant:           http://localhost:6333"
	@echo "Qdrant Dashboard: http://localhost:6333/dashboard"
	@echo "Ollama:           http://localhost:11434"
	@echo ""
	@echo "$(YELLOW)⏳ First startup: Wait 5-10 minutes for Ollama to download models$(NC)"
	@echo "   - nomic-embed-text (~270MB)"
	@echo "   - llama3.1 (~4.7GB)"

stop: ## Stop all services
	@echo "$(YELLOW)🛑 Stopping services...$(NC)"
	docker-compose down
	@echo "$(GREEN)✅ Services stopped$(NC)"

restart: ## Restart all services
	@echo "$(YELLOW)🔄 Restarting services...$(NC)"
	docker-compose restart
	@echo "$(GREEN)✅ Services restarted$(NC)"

logs: ## Show logs from all services
	docker-compose logs -f

logs-api: ## Show API logs
	docker-compose logs -f api

logs-ollama: ## Show Ollama logs
	docker-compose logs -f ollama

logs-qdrant: ## Show Qdrant logs
	docker-compose logs -f qdrant

logs-blazor: ## Show Blazor UI logs
	docker-compose logs -f blazor

status: ## Show status of all services
	@echo "$(BLUE)📊 Service Status:$(NC)"
	docker-compose ps

rebuild: ## Rebuild and restart API
	@echo "$(YELLOW)🔨 Rebuilding API...$(NC)"
	docker-compose build --no-cache api
	docker-compose up -d api
	@echo "$(GREEN)✅ API rebuilt and restarted$(NC)"

clean: ## Stop and remove all containers, networks, and volumes (DESTRUCTIVE)
	@echo "$(YELLOW)⚠️  This will remove ALL data (documents, registry, Qdrant collection)$(NC)"
	@read -p "Are you sure? (yes/no): " confirm; \
	if [ "$$confirm" = "yes" ]; then \
		docker-compose down -v; \
		echo "$(GREEN)✅ Cleanup completed$(NC)"; \
	else \
		echo "$(BLUE)Operation cancelled$(NC)"; \
	fi

backup: ## Backup all volumes
	@echo "$(BLUE)💾 Creating backups...$(NC)"
	@mkdir -p backups
	@timestamp=$$(date +%Y%m%d_%H%M%S); \
	docker run --rm -v nsaquestionanswerer_qdrant_storage:/data -v $$(pwd)/backups:/backup alpine tar czf /backup/qdrant-$$timestamp.tar.gz -C /data .; \
	docker run --rm -v nsaquestionanswerer_api_storage:/data -v $$(pwd)/backups:/backup alpine tar czf /backup/api-storage-$$timestamp.tar.gz -C /data .; \
	echo "$(GREEN)✅ Backups created in ./backups/$(NC)"; \
	ls -lh backups/*$$timestamp*
api/documents/upload" -F "file=@example-document.md" | jq .
	@echo ""
	@echo "$(YELLOW)⏳ Waiting 10 seconds for indexing...$(NC)"
	@sleep 10
	@echo "$(BLUE)📋 Current documents:$(NC)"
	@curl -s "http://localhost:5000/api/documents" | jq .

test-ask: ## Test question answering (requires indexed documents)
	@echo "$(BLUE)🤔 Asking: 'What is this document about?'$(NC)"
	@curl -s -X POST "http://localhost:5000/api/questions/ask" \
		-H "Content-Type: application/json" \
		-d '{"question": "What is this document about?"}' | jq .

test-pipeline2: ## Complete Pipeline 2 test (upload + index + ask)
	@echo "$(GREEN)🧪 Testing complete Pipeline 2...$(NC)"
	@echo ""
	@echo "$(BLUE)Step 1: Upload document$(NC)"
	@curl -s -X POST "http://localhost:5000/api/documents/upload" -F "file=@example-document.md" | jq .
	@echo ""
	@echo "$(YELLOW)⏳ Waiting 15 seconds for indexing...$(NC)"
	@sleep 15
	@echo ""
	@echo "$(BLUE)Step 2: Check document status$(NC)"
	@curl -s "http://localhost:5000/api/documents" | jq '.[0] | {fileName, status, indexedAtUtc}'
	@echo ""
	@echo "$(BLUE)Step 3: Ask a question$(NC)"
	@curl -s -X POST "http://localhost:5000/api/questions/ask" \
		-H "Content-Type: application/json" \
		-d '{"question": "What is this document about?"}' | jq '{answer: .answer, sourcesFound: .sourcesFound, tokensUsed: .tokensUsed}'
	@echo ""
	@echo "$(GREEN)✅ Pipeline 2 test complete!$(NC)"...$(NC)"
	@sleep 10
	@echo "$(BLUE)📋 Current documents:$(NC)"
	@curl -s "http://localhost:5000/documents" | jq .

check: ## Check system health (Ollama models, Qdrant collections)
	@echo "$(BLUE)🤖 Ollama models:$(NC)"
	@docker exec ollama ollama list
	@echo ""
	@echo "$(BLUE)🗄️  Qdrant collections:$(NC)"
	@curl -s "http://localhost:6333/collections" | jq .
	@echo ""
	@echo "$(BLUE)📊 Qdrant technical_docs collection:$(NC)"
	@curl -s "http://localhost:6333/collections/technical_docs" | jq . 2>/dev/null || echo "Collection not created yet"

dev: ## Start development environment with hot-reload
	@echo "$(GREEN)🔨 Starting development environment with hot-reload...$(NC)"
	docker-compose -f docker-compose.dev.yml up --build

dev-stop: ## Stop development environment
	docker-compose -f docker-compose.dev.yml down

shell-api: ## Open shell in API container
	docker exec -it technicaldocsqa-api /bin/bash

shell-ollama: ## Open shell in Ollama container
	docker exec -it ollama /bin/bash

.DEFAULT_GOAL := help
