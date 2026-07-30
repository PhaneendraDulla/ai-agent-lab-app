# RAG Pipeline — Testing Notes

Manual test session covering the Milestone 2 RAG pipeline (document ingestion, embeddings,
vector store, retrieval-augmented chat). Kept for reference when re-testing after future changes.

## Setup

- Embedding model: `models/gemini-embedding-001` (768-dim, via `outputDimensionality`)
- Vector store: `Sql` (`SqlVectorStore`, brute-force cosine similarity)
- `VectorStore:TopK` = 4
- `VectorStore:MinScore` = 0.6 (tuned down from an initial 0.7 — see findings below)
- `Gemini:Temperature` = 0.2
- Test documents ingested into `Documents/`:
  - `rag-overview.md` — general RAG explainer (2 chunks)
  - `stock-info.md` — "Internal Watchlist" doc mentioning MSFT
  - `test-fact.md` — "Internal Test Note" with a fabricated fact (codename "Velvet Marigold", lead "Priya Chen") used to prove answers come from retrieval, not the model's own training data

## Bugs found and fixed during this session

1. **DI lifetime mismatch** — `IVectorStoreFactory` / `IEmbeddingProviderFactory` were registered
   `Singleton` but resolved `Scoped` services (`SqlVectorStore`, `GeminiEmbeddingProvider`),
   causing `Cannot resolve scoped service from root provider` on the first `/api/documents/ingest`
   call. Fixed by registering both factories as `Scoped`.
2. **Wrong embedding model** — `models/text-embedding-004` returns 404 (retired). Switched to
   `models/gemini-embedding-001` with `outputDimensionality: 768` to keep vector size consistent
   with `Gemini:EmbeddingDimensions` and the Qdrant collection config.
3. **No score threshold** — originally `SqlVectorStore.SearchAsync` always returned exactly
   `TopK` chunks regardless of relevance, so unrelated questions still got irrelevant context
   injected into the prompt. Added `VectorStoreSettings.MinScore` and filtered in
   `ChatService.RetrieveRagContextAsync`.
4. **No temperature set** — Gemini calls had no `generationConfig.temperature`, defaulting to
   Gemini's `1.0`. Added `Gemini:Temperature` (0.2) for more deterministic, grounded answers.
5. **Known but not yet fixed: stale chunk bug** — re-ingesting a document that *shrinks* (fewer
   chunks than before) leaves orphaned rows in `VectorChunks` for chunk indices that no longer
   exist, since `SqlVectorStore.UpsertAsync` only updates/inserts by ID, never deletes.
   Growing/same-size edits are safe; shrinking is not. **Not tested/fixed as of this session.**

## Debug logging added (temporary)

`ChatService.RetrieveRagContextAsync` logs every candidate chunk and its cosine similarity
score *before* the `MinScore` filter runs, tagged `RAG DEBUG` and marked with a
`TEMP DEBUG (remove once RAG tuning is done)` comment at `ChatService.cs` — remove once
threshold tuning is finalized.

## Test scenarios covered

### 1. Basic ingestion
- `POST /api/documents/ingest` against `rag-overview.md` alone — confirmed single-chunk
  ingestion (`ChunkIndex 0`) since the doc is well under `ChunkSizeTokens: 500`.
- Confirmed re-running ingest overwrites the same chunk ID rather than duplicating (deterministic
  `{documentName}::{chunkIndex}` IDs).

### 2. Grounded fact test (proves retrieval, not model knowledge)
- Question: *"What is the launch codename for the Q3 project, and what's the support ticket
  prefix?"* (answer only exists in `test-fact.md`, a fabricated doc)
- Result: correctly answered "Velvet Marigold" / "VMT-" — confirms the answer came from the
  retrieved chunk, not the model's own training data.

### 3. Score threshold behavior
- Unrelated question against a 0.7 threshold correctly showed
  `RAG: N chunk(s) retrieved but none met the 0.7 similarity threshold` and fell back to general
  model knowledge.
- Found `test-fact.md` scoring 0.62–0.65 even on queries unrelated to it — appears to be a
  similarity "floor" for short, similarly-styled internal docs rather than true topical
  relevance. Lowered threshold to 0.6 as a working value; **not fully confirmed clean** — see
  open follow-up below.

### 4. RAG feeding a tool call (combined scenario)
- Question: *"According to our internal watchlist, what stock are we tracking, and what is its
  current price?"*
- Confirmed: RAG retrieved `stock-info.md` (score 0.78), model read "MSFT" from the injected
  context, and correctly called `get_stock_price` with `{"symbol":"MSFT"}` — i.e. retrieved
  document content successfully drove a tool call argument, not just a text answer.
- Confirmed 2-iteration tool loop: iteration 1 calls the tool, iteration 2 returns the final
  answer using the tool result.

### 5. Conversation memory + RAG across turns
- Same `conversationId` across two turns.
- Turn 2 (*"What stock does our internal watchlist mention?"*) showed
  `MEMORY: loaded 2 prior message(s)`, confirming turn 1's user+assistant messages persisted and
  replayed correctly.
- Turn 2 correctly did **not** trigger a tool call (only asked about doc content, not a live
  price) — confirms the model isn't over-triggering tools when RAG context alone answers the
  question.

## Open follow-ups (not yet done)

- **Fix the stale chunk bug** (delete-by-document before re-insert on ingestion).
- **Confirm `test-fact.md` isn't leaking into unrelated answers** — need to inspect the actual
  `answer` text (not just logs) from scenarios where it crossed the 0.6 threshold but wasn't
  actually relevant.
- **Multi-chunk document test** — haven't yet ingested a document long enough (1500+ tokens) to
  produce more than 1-2 chunks and confirm overlap/retrieval across chunk boundaries.
- **Restart-without-re-ingest test** — confirm chat retrieval still works against
  previously-ingested chunks after an app restart (proves DB persistence, not in-memory state).
- **Remove temporary `RAG DEBUG` logging** once threshold tuning is considered final.
