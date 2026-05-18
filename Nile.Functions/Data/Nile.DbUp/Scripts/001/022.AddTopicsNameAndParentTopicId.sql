-- 022.AddTopicsNameAndParentTopicId.sql
-- Purpose: Ensure dbo.Topics has [Name] and [ParentTopicId] columns expected by the app
-- Idempotent: safe to run multiple times

IF OBJECT_ID('dbo.Topics', 'U') IS NULL
BEGIN
    PRINT '022: Skipping — dbo.Topics table not found.'
    RETURN;
END

-- Add [Name] NVARCHAR(4000) NULL if missing
IF COL_LENGTH('dbo.Topics', 'Name') IS NULL
BEGIN
    PRINT '022: Adding dbo.Topics.[Name] NVARCHAR(4000) NULL';
    ALTER TABLE dbo.Topics ADD [Name] NVARCHAR(4000) NULL;
END
ELSE
BEGIN
    PRINT '022: dbo.Topics.[Name] already exists — skipping';
END

-- Add [ParentTopicId] UNIQUEIDENTIFIER NULL if missing
IF COL_LENGTH('dbo.Topics', 'ParentTopicId') IS NULL
BEGIN
    PRINT '022: Adding dbo.Topics.[ParentTopicId] UNIQUEIDENTIFIER NULL';
    ALTER TABLE dbo.Topics ADD [ParentTopicId] UNIQUEIDENTIFIER NULL;

    -- Helpful nonclustered index for hierarchy lookups
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE name = 'IX_Topics_ParentTopicId' AND object_id = OBJECT_ID('dbo.Topics')
    )
    BEGIN
        CREATE INDEX IX_Topics_ParentTopicId ON dbo.Topics(ParentTopicId);
    END

    -- Optional FK to self (disabled by default until data is clean). Uncomment if you want to enforce referential integrity.
    --IF NOT EXISTS (
    --    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Topics_Topics_Parent' AND parent_object_id = OBJECT_ID('dbo.Topics')
    --)
    --BEGIN
    --    ALTER TABLE dbo.Topics
    --      ADD CONSTRAINT FK_Topics_Topics_Parent FOREIGN KEY (ParentTopicId) REFERENCES dbo.Topics(TopicId);
    --END
END
ELSE
BEGIN
    PRINT '022: dbo.Topics.[ParentTopicId] already exists — skipping';
END

PRINT '022: AddTopicsNameAndParentTopicId completed.';
