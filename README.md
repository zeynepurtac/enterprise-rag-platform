# Enterprise On-Prem RAG & Document Intelligence Platform

A production-style, fully self-hosted **Retrieval-Augmented Generation (RAG)**
platform. Upload PDFs, have them chunked, embedded and indexed into a vector
database, then **chat with your documents** and get answers grounded in the
source text with **page-level citations** — all running on your own
infrastructure, with **no data leaving your network**.

The entire stack comes up with a single `docker compose up`.

---

## ✨ Highlights

- **On-prem by default** — local LLM + embeddings via [Ollama](https://ollama.com);
  swap to any OpenAI-compatible endpoint with config only, no code changes.
- **Clean Architecture .NET 9 backend** — Domain / Application / Infrastructure /
  API layering, dependency inversion, and clear separation of concerns.
- **Semantic search over Qdrant** — cosine similarity retrieval with metadata
  filtering, so answers can be scoped to a single document or the whole corpus.
- **Streaming answers** — token-by-token responses over Server-Sent Events.
- **Grounded citations** — every answer surfaces the exact source file, page and
  a snippet, with a relevance score.
- **Asynchronous ingestion** — uploads return instantly; extraction, embedding
  and indexing run on a background worker with live status in the UI.
- **Modern Angular UI** — drag-and-drop upload, document management and a chat
  workspace.

---

## 🏗️ Architecture

```mermaid
flowchart LR
    subgraph Client
        UI["Angular SPA<br/>(nginx)"]
    end

    subgraph API["ASP.NET Core 9 Web API"]
        DC["Documents<br/>Controller"]
        CC["Chat<br/>Controller"]
        Q["Background<br/>Ingestion Queue"]
        RAG["RAG Service"]
    end

    subgraph Pipeline["Ingestion Pipeline"]
        PP["PDF Processing<br/>(iText7 + chunking)"]
        EMB["Embedding Service"]
    end

    subgraph Data["Data & AI Services"]
        PG[("PostgreSQL<br/>metadata")]
        QD[("Qdrant<br/>vectors")]
        OL["Ollama<br/>LLM + embeddings"]
    end

    UI -- "REST / SSE" --> DC
    UI -- "REST / SSE" --> CC
    DC --> Q
    Q --> PP --> EMB
    EMB -- "embeddings" --> OL
    EMB -- "upsert vectors" --> QD
    DC -- "document rows" --> PG

    CC --> RAG
    RAG -- "embed query" --> OL
    RAG -- "similarity search" --> QD
    RAG -- "grounded prompt" --> OL
    CC -- "chat history" --> PG
```

### Request lifecycle

1. **Upload** — `POST /api/documents` stores the PDF, creates a `Pending`
   record and enqueues a background job (returns `202 Accepted` immediately).
2. **Ingest** — the worker extracts text per page (iText 7), splits it into
   overlapping token-bounded chunks, embeds each chunk and upserts the vectors
   into Qdrant. Status transitions `Pending → Processing → Completed/Failed`.
3. **Ask** — `POST /api/chat/stream` embeds the question, retrieves the top-K
   most similar chunks (optionally filtered to one document), builds a grounded
   prompt and streams the model's answer back with citations.

---

## 🧱 Tech Stack

| Layer          | Technology                                                        |
| -------------- | ----------------------------------------------------------------- |
| Frontend       | Angular 19 (standalone components, signals), nginx                |
| Backend        | .NET 9, ASP.NET Core Web API, Clean Architecture                  |
| ORM / Metadata | Entity Framework Core 9, PostgreSQL (SQLite for local dev)        |
| Vector store   | Qdrant (HTTP REST API, cosine distance)                           |
| LLM & Embeds   | Ollama (`llama3.2`, `nomic-embed-text`) — OpenAI-compatible       |
| PDF extraction | iText 7                                                    |
| API docs       | Swagger / OpenAPI (Swashbuckle)                                   |
| Orchestration  | Docker & Docker Compose                                           |

---

## 🚀 Getting Started

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose v2
- ~8 GB free disk space for the model + images (first run only)
- (Optional) an NVIDIA GPU + Container Toolkit for faster inference

### Run it

```bash
git clone <your-repo-url> enterprise-rag-platform
cd enterprise-rag-platform

# Optional: customise ports, credentials or models
cp .env.example .env

docker compose up --build
```

On the **first** start, the `ollama-init` service downloads the chat and
embedding models (a few GB). This is a one-time cost — subsequent starts are
fast because the models are cached in a named volume. You can watch progress
with `docker compose logs -f ollama-init`.

Once everything is healthy:

| Service        | URL                                            |
| -------------- | ---------------------------------------------- |
| **Web app**    | http://localhost:8081                          |
| **API**        | http://localhost:8080                          |
| **Swagger UI** | http://localhost:8080/swagger                  |
| **Qdrant**     | http://localhost:6333/dashboard                |

Upload a PDF, wait for its status to reach **Completed**, then start asking
questions.

---

## 🔌 API Reference

| Method   | Endpoint                 | Description                                   |
| -------- | ------------------------ | --------------------------------------------- |
| `GET`    | `/api/documents`         | List all documents (newest first)             |
| `GET`    | `/api/documents/{id}`    | Get a single document + processing status     |
| `POST`   | `/api/documents`         | Upload a PDF (`multipart/form-data`, `file`)  |
| `DELETE` | `/api/documents/{id}`    | Delete a document and its vectors             |
| `POST`   | `/api/chat`              | Ask a question (buffered JSON response)       |
| `POST`   | `/api/chat/stream`       | Ask a question (Server-Sent Events streaming) |
| `GET`    | `/health`                | Liveness probe                                |

### Example: ask a question

```bash
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
        "question": "What is the termination clause?",
        "documentId": null,
        "history": []
      }'
```

---

## ⚙️ Configuration

All settings are overridable via environment variables using the standard
ASP.NET Core `__` (double-underscore) convention. The most useful ones:

| Variable                    | Default                    | Purpose                              |
| --------------------------- | -------------------------- | ------------------------------------ |
| `Ai__BaseUrl`               | `http://ollama:11434/v1`   | OpenAI-compatible LLM base URL       |
| `Ai__ApiKey`                | *(empty)*                  | Bearer key (required for OpenAI)     |
| `Ai__ChatModel`             | `llama3.2`                 | Chat / generation model              |
| `Ai__EmbeddingModel`        | `nomic-embed-text`         | Embedding model                      |
| `Ai__EmbeddingDimensions`   | `768`                      | Must match the embedding model       |
| `Qdrant__BaseUrl`           | `http://qdrant:6333`       | Vector database endpoint             |
| `Database__Provider`        | `postgres`                 | `postgres` or `sqlite`               |
| `Rag__TopK`                 | `5`                        | Chunks retrieved per question        |
| `Rag__MinScore`             | `0.25`                     | Minimum cosine score to use a chunk  |
| `Chunking__MaxTokens`       | `450`                      | Target chunk size (approx tokens)    |
| `Chunking__OverlapTokens`   | `80`                       | Overlap between consecutive chunks   |

### Using OpenAI instead of Ollama

Because the backend targets the OpenAI-compatible surface, switching providers
is purely configuration. Set on the `backend` service:

```yaml
Ai__BaseUrl: https://api.openai.com/v1
Ai__ApiKey: sk-...
Ai__ChatModel: gpt-4o-mini
Ai__EmbeddingModel: text-embedding-3-small
Ai__EmbeddingDimensions: 1536
```

---

## 🧑‍💻 Local Development (without Docker)

**Backend** (uses SQLite + your local Ollama automatically via the
`Development` profile):

```bash
cd backend
dotnet run --project src/RagPlatform.Api
# Swagger at http://localhost:8080/swagger
```

**Frontend**:

```bash
cd frontend
npm install
npm start
# App at http://localhost:4200 (proxies to the backend at :8080)
```

You'll need Ollama running locally (`ollama serve`) with the two models pulled
(`ollama pull llama3.2 && ollama pull nomic-embed-text`) and a Qdrant instance
(`docker run -p 6333:6333 qdrant/qdrant`).

---

## 📂 Project Structure

```
enterprise-rag-platform/
├── docker-compose.yml            # Full stack orchestration
├── .env.example                  # Configurable ports, creds, models
├── backend/                      # .NET 9 Clean Architecture solution
│   ├── Dockerfile
│   └── src/
│       ├── RagPlatform.Domain/           # Entities, enums (no dependencies)
│       ├── RagPlatform.Application/      # Interfaces, DTOs, models
│       ├── RagPlatform.Infrastructure/   # EF Core, Qdrant, Ollama, RAG, ingestion
│       └── RagPlatform.Api/              # Controllers, Program.cs, Swagger
└── frontend/                     # Angular 19 SPA
    ├── Dockerfile
    ├── nginx.conf                # SPA routing + /api reverse proxy (SSE-aware)
    └── src/app/
        ├── core/                 # Models & services (HTTP, streaming, state)
        └── features/             # Documents pane + Chat pane
```

---

## 🔍 How Retrieval-Augmented Generation Works Here

1. **Chunking with overlap** keeps related sentences together and preserves
   context across chunk boundaries, improving retrieval quality.
2. **Per-page chunking** means every vector remembers which page it came from,
   so citations point to a real location in the source PDF.
3. **A relevance floor** (`Rag__MinScore`) discards weak matches so the model
   isn't fed irrelevant context — if nothing clears the bar, the platform says
   so instead of hallucinating.
4. **A grounded system prompt** instructs the model to answer only from the
   provided context and to cite sources by number.

---

## 📝 License

Released under the [MIT License](LICENSE).
