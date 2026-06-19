# SQL schema for AiAgentLab

This folder contains SQL Server schema scripts and helpers for local development.

Files
- `schema.sql` — full DDL for Users, Conversations, Messages, MessageEmbeddings, tags, and a helper stored procedure.

How to run
1. Open `schema.sql` in SQL Server Management Studio (SSMS) connected to your local SQL Server (or LocalDB).
2. Run the script. It will create the `AiAgentLabDb` database if it does not already exist.

Notes
- The schema uses `UNIQUEIDENTIFIER` (GUID) primary keys generated with `NEWSEQUENTIALID()`.
- Metadata columns store JSON as `NVARCHAR(MAX)` so you can use SQL Server JSON functions like `JSON_VALUE` and `OPENJSON`.
- For production or high-scale vector search, consider a dedicated vector database; `MessageEmbeddings` is RAG-ready as a placeholder.

Next steps
- Add EF Core entities and `AppDbContext` to map to these tables.
- Create EF Core migrations or use these scripts directly in environments where you manage schema via DBA processes.
