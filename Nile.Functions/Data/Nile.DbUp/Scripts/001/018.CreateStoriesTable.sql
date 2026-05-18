-- Migration: Create Stories table
-- Idempotent: only create table if missing

IF OBJECT_ID('dbo.Stories') IS NULL
BEGIN
    CREATE TABLE dbo.Stories (
        StoryId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SubjectId UNIQUEIDENTIFIER NULL,
        TopicId UNIQUEIDENTIFIER NULL,
        PromptId NVARCHAR(200) NULL,
        [User] NVARCHAR(200) NULL,
        Payload NVARCHAR(MAX) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_Stories_IsDeleted DEFAULT (0),
        DeletedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );

    CREATE INDEX IDX_Stories_School_Subject ON dbo.Stories (SchoolId, SubjectId);
    CREATE INDEX IDX_Stories_CreatedAt ON dbo.Stories (CreatedAt);
END
ELSE
BEGIN
    PRINT 'Skipping Stories creation: dbo.Stories already exists.';
END
GO

PRINT 'Stories migration applied.';
