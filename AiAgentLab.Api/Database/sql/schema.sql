-- ============================================================
--  AiAgentLab -- Clean SQL Server Schema
--  Users.Id   : INT IDENTITY (1, 2, 3 ...)
--  All other PKs: NVARCHAR(450) GUID strings (EF Core pattern)
--  Run this ONCE on a fresh database
--  Date: 2026-06-19
-- ============================================================

-- ============================================================
--  1. CREATE DATABASE
-- ============================================================
IF DB_ID(N'AiAgentLabDb') IS NULL
BEGIN
    CREATE DATABASE AiAgentLabDb;
    PRINT 'Database AiAgentLabDb created.';
END
ELSE
    PRINT 'Database AiAgentLabDb already exists, skipping.';
GO

USE AiAgentLabDb;
GO

-- ============================================================
--  2. DROP existing objects (safe re-run, FK order)
-- ============================================================
IF OBJECT_ID('dbo.CreateConversationWithMessage', 'P') IS NOT NULL DROP PROCEDURE dbo.CreateConversationWithMessage;
IF OBJECT_ID('dbo.GetConversationHistory',        'P') IS NOT NULL DROP PROCEDURE dbo.GetConversationHistory;
IF OBJECT_ID('dbo.GetUserConversations',          'P') IS NOT NULL DROP PROCEDURE dbo.GetUserConversations;
GO

IF OBJECT_ID('dbo.MessageEmbeddings', 'U') IS NOT NULL DROP TABLE dbo.MessageEmbeddings;
IF OBJECT_ID('dbo.ConversationTags',  'U') IS NOT NULL DROP TABLE dbo.ConversationTags;
IF OBJECT_ID('dbo.Messages',          'U') IS NOT NULL DROP TABLE dbo.Messages;
IF OBJECT_ID('dbo.Conversations',     'U') IS NOT NULL DROP TABLE dbo.Conversations;
IF OBJECT_ID('dbo.Users',             'U') IS NOT NULL DROP TABLE dbo.Users;
GO

PRINT 'Existing objects dropped.';
GO

-- ============================================================
--  3. USERS
--     Id is INT IDENTITY - auto-increments as 1, 2, 3 ...
-- ============================================================
CREATE TABLE dbo.Users
(
    Id          INT             NOT NULL IDENTITY(1,1),
    Username    NVARCHAR(200)   NOT NULL,
    Email       NVARCHAR(320)   NULL,
    DisplayName NVARCHAR(256)   NULL,
    ProviderId  NVARCHAR(256)   NULL,
    IsActive    BIT             NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastSeenAt  DATETIME2       NULL,

    CONSTRAINT PK_Users PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users (Username);
CREATE UNIQUE INDEX UX_Users_Email    ON dbo.Users (Email) WHERE Email IS NOT NULL;

PRINT 'Table dbo.Users created.';
GO

-- ============================================================
--  4. CONVERSATIONS
--     UserId is INT to match Users.Id
-- ============================================================
CREATE TABLE dbo.Conversations
(
    Id         NVARCHAR(450)   NOT NULL,
    UserId     INT             NOT NULL,
    Title      NVARCHAR(512)   NULL,
    IsArchived BIT             NOT NULL DEFAULT 0,
    CreatedAt  DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt  DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Conversations PRIMARY KEY (Id),
    CONSTRAINT FK_Conversations_Users
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);

CREATE INDEX IX_Conversations_UserId    ON dbo.Conversations (UserId);
CREATE INDEX IX_Conversations_UpdatedAt ON dbo.Conversations (UpdatedAt DESC);

PRINT 'Table dbo.Conversations created.';
GO

-- ============================================================
--  5. MESSAGES
-- ============================================================
CREATE TABLE dbo.Messages
(
    Id               NVARCHAR(450)   NOT NULL,
    ConversationId   NVARCHAR(450)   NOT NULL,
    Role             NVARCHAR(50)    NOT NULL,
    Content          NVARCHAR(MAX)   NOT NULL,
    CreatedAt        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    IntentDomain     NVARCHAR(100)   NULL,
    IntentAction     NVARCHAR(100)   NULL,
    IntentConfidence FLOAT           NULL,
    Metadata         NVARCHAR(MAX)   NULL,
    IsDeleted        BIT             NOT NULL DEFAULT 0,

    CONSTRAINT PK_Messages PRIMARY KEY (Id),
    CONSTRAINT FK_Messages_Conversations
        FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations (Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Messages_Role
        CHECK (Role IN ('user', 'assistant', 'system'))
);

CREATE INDEX IX_Messages_ConversationId_CreatedAt
    ON dbo.Messages (ConversationId, CreatedAt);

PRINT 'Table dbo.Messages created.';
GO

-- ============================================================
--  6. MESSAGE EMBEDDINGS  (RAG placeholder - Milestone 2)
-- ============================================================
CREATE TABLE dbo.MessageEmbeddings
(
    Id        NVARCHAR(450)   NOT NULL,
    MessageId NVARCHAR(450)   NOT NULL,
    Embedding VARBINARY(MAX)  NOT NULL,
    VectorDim INT             NOT NULL,
    ModelName NVARCHAR(200)   NULL,
    CreatedAt DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_MessageEmbeddings PRIMARY KEY (Id),
    CONSTRAINT FK_MessageEmbeddings_Messages
        FOREIGN KEY (MessageId) REFERENCES dbo.Messages (Id) ON DELETE CASCADE
);

CREATE INDEX IX_MessageEmbeddings_MessageId ON dbo.MessageEmbeddings (MessageId);

PRINT 'Table dbo.MessageEmbeddings created.';
GO

-- ============================================================
--  7. CONVERSATION TAGS
-- ============================================================
CREATE TABLE dbo.ConversationTags
(
    ConversationId NVARCHAR(450)   NOT NULL,
    Tag            NVARCHAR(100)   NOT NULL,
    CreatedAt      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ConversationTags PRIMARY KEY (ConversationId, Tag),
    CONSTRAINT FK_ConversationTags_Conversations
        FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations (Id) ON DELETE CASCADE
);

PRINT 'Table dbo.ConversationTags created.';
GO

-- ============================================================
--  8. SP: CreateConversationWithMessage
-- ============================================================
CREATE PROCEDURE dbo.CreateConversationWithMessage
    @ConversationId NVARCHAR(450),
    @UserId         INT,
    @Title          NVARCHAR(512) = NULL,
    @MessageId      NVARCHAR(450),
    @Role           NVARCHAR(50),
    @Content        NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY

        INSERT INTO dbo.Conversations (Id, UserId, Title, CreatedAt, UpdatedAt)
        VALUES (@ConversationId, @UserId, @Title, SYSUTCDATETIME(), SYSUTCDATETIME());

        INSERT INTO dbo.Messages (Id, ConversationId, Role, Content, CreatedAt)
        VALUES (@MessageId, @ConversationId, @Role, @Content, SYSUTCDATETIME());

        COMMIT TRANSACTION;
        SELECT @ConversationId AS ConversationId, @MessageId AS MessageId;

    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
        RETURN -1;
    END CATCH
END
GO

PRINT 'SP dbo.CreateConversationWithMessage created.';
GO

-- ============================================================
--  9. SP: GetConversationHistory
-- ============================================================
CREATE PROCEDURE dbo.GetConversationHistory
    @ConversationId NVARCHAR(450),
    @MaxMessages    INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@MaxMessages)
        m.Id,
        m.ConversationId,
        m.Role,
        m.Content,
        m.CreatedAt,
        m.IntentDomain,
        m.IntentAction,
        m.IntentConfidence,
        m.Metadata
    FROM dbo.Messages m
    WHERE
        m.ConversationId = @ConversationId
        AND m.IsDeleted   = 0
    ORDER BY m.CreatedAt DESC;
END
GO

PRINT 'SP dbo.GetConversationHistory created.';
GO

-- ============================================================
-- 10. SP: GetUserConversations
-- ============================================================
CREATE PROCEDURE dbo.GetUserConversations
    @UserId     INT,
    @PageSize   INT = 50,
    @PageNumber INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        c.Id,
        c.Title,
        c.CreatedAt,
        c.UpdatedAt,
        c.IsArchived
    FROM dbo.Conversations c
    WHERE
        c.UserId     = @UserId
        AND c.IsArchived = 0
    ORDER BY c.UpdatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

PRINT 'SP dbo.GetUserConversations created.';
GO

-- ============================================================
-- 11. SEED: default users so you can test immediately
-- ============================================================
INSERT INTO dbo.Users (Username, Email, DisplayName, IsActive)
VALUES
    ('admin',   'admin@localhost',   'Admin',   1),  -- Id = 1
    ('alice',   'alice@localhost',   'Alice',   1),  -- Id = 2
    ('bob',     'bob@localhost',     'Bob',     1);  -- Id = 3

PRINT 'Seed users inserted (Ids 1, 2, 3).';
GO

-- ============================================================
-- 12. VERIFY
-- ============================================================
SELECT TABLE_NAME   FROM INFORMATION_SCHEMA.TABLES   WHERE TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME;
SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES  WHERE ROUTINE_TYPE = 'PROCEDURE' ORDER BY ROUTINE_NAME;
SELECT Id, Username FROM dbo.Users;
GO

PRINT '=== Schema created successfully. ===';
GO









SELECT Role, Content, ConversationId, CreatedAt FROM dbo.Messages ORDER BY CreatedAt;

SELECT * FROM dbo.Users;
SELECT * FROM dbo.Conversations;
SELECT * FROM dbo.ConversationTags;
SELECT * FROM dbo.MessageEmbeddings;
SELECT * FROM dbo.Messages ;



