-- AiAgentLab SQL schema
-- Create database (optional)
IF DB_ID(N'AiAgentLabDb') IS NULL
BEGIN
    CREATE DATABASE AiAgentLabDb;
END
GO

USE AiAgentLabDb;
GO

-- Users table
CREATE TABLE dbo.Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Username NVARCHAR(200) NOT NULL,
    Email NVARCHAR(320) NULL,
    DisplayName NVARCHAR(256) NULL,
    ProviderId NVARCHAR(256) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastSeenAt DATETIME2 NULL
);
CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email) WHERE Email IS NOT NULL;

-- Conversations table
CREATE TABLE dbo.Conversations (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(512) NULL,
    IsArchived BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Conversations_UserId ON dbo.Conversations(UserId);
ALTER TABLE dbo.Conversations
ADD CONSTRAINT FK_Conversations_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;

-- Messages table
CREATE TABLE dbo.Messages (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IntentDomain NVARCHAR(100) NULL,
    IntentAction NVARCHAR(100) NULL,
    IntentConfidence FLOAT NULL,
    Metadata NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
CREATE INDEX IX_Messages_ConversationId_CreatedAt ON dbo.Messages(ConversationId, CreatedAt);
ALTER TABLE dbo.Messages
ADD CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id) ON DELETE CASCADE;

-- MessageEmbeddings table (RAG-ready)
CREATE TABLE dbo.MessageEmbeddings (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    MessageId UNIQUEIDENTIFIER NOT NULL,
    Embedding VARBINARY(MAX) NOT NULL,
    VectorDim INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_MessageEmbeddings_MessageId ON dbo.MessageEmbeddings(MessageId);
ALTER TABLE dbo.MessageEmbeddings
ADD CONSTRAINT FK_MessageEmbeddings_Message FOREIGN KEY (MessageId) REFERENCES dbo.Messages(Id) ON DELETE CASCADE;

-- ConversationTags (optional)
CREATE TABLE dbo.ConversationTags (
    ConversationId UNIQUEIDENTIFIER NOT NULL,
    Tag NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (ConversationId, Tag)
);
ALTER TABLE dbo.ConversationTags
ADD CONSTRAINT FK_ConversationTags_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id) ON DELETE CASCADE;

-- Stored procedure: create conversation + first message
CREATE PROCEDURE dbo.CreateConversationWithMessage
    @UserId UNIQUEIDENTIFIER,
    @Title NVARCHAR(512) = NULL,
    @Role NVARCHAR(50),
    @Content NVARCHAR(MAX),
    @OutConversationId UNIQUEIDENTIFIER OUTPUT,
    @OutMessageId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Insert conversation while letting the table default generate the Id (NEWSEQUENTIALID())
        DECLARE @InsertedConvo TABLE (Id UNIQUEIDENTIFIER);
        INSERT INTO dbo.Conversations (UserId, Title, CreatedAt, UpdatedAt)
        OUTPUT inserted.Id INTO @InsertedConvo
        VALUES (@UserId, @Title, SYSUTCDATETIME(), SYSUTCDATETIME());

        SELECT @OutConversationId = Id FROM @InsertedConvo;

        -- Insert message while letting the table default generate the Id
        DECLARE @InsertedMsg TABLE (Id UNIQUEIDENTIFIER);
        INSERT INTO dbo.Messages (ConversationId, Role, Content, CreatedAt)
        OUTPUT inserted.Id INTO @InsertedMsg
        VALUES (@OutConversationId, @Role, @Content, SYSUTCDATETIME());

        SELECT @OutMessageId = Id FROM @InsertedMsg;

        COMMIT TRANSACTION;
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
        RETURN -1;
    END CATCH
END
GO

-- Sample queries (sliding window)
-- Last 20 messages for a conversation
-- SELECT TOP (20) m.Id, m.Role, m.Content, m.CreatedAt, m.IntentDomain, m.IntentAction, m.IntentConfidence, m.Metadata
-- FROM dbo.Messages m
-- WHERE m.ConversationId = @conversationId AND m.IsDeleted = 0
-- ORDER BY m.CreatedAt DESC;

-- List user conversations (recent first)
-- SELECT c.Id, c.Title, c.UpdatedAt
-- FROM dbo.Conversations c
-- WHERE c.UserId = @userId AND c.IsArchived = 0
-- ORDER BY c.UpdatedAt DESC
-- OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;

-- Notes:
-- - Use NEWSEQUENTIALID() for keys to reduce index fragmentation on heavy inserts.
-- - Store metadata as JSON in Metadata column and use SQL Server JSON functions when needed.
-- - For embeddings, prefer an external vector DB for scale and store references here.
