-- Create SubTopics table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubTopics')
BEGIN
    CREATE TABLE dbo.SubTopics (
        SubTopicId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TopicId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        Notes NVARCHAR(MAX) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );

    -- Add FK only if the Topics table exists and the FK isn't already present
    IF OBJECT_ID('dbo.Topics', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SubTopics_Topics_TopicId')
    BEGIN
        ALTER TABLE dbo.SubTopics
            ADD CONSTRAINT FK_SubTopics_Topics_TopicId FOREIGN KEY (TopicId) REFERENCES dbo.Topics(TopicId) ON DELETE CASCADE;
    END

    -- Create unique index only if it doesn't already exist
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SubTopics_TopicId_Name' AND object_id = OBJECT_ID('dbo.SubTopics'))
    BEGIN
        CREATE UNIQUE INDEX IX_SubTopics_TopicId_Name ON dbo.SubTopics(TopicId, Name);
    END
END

-- Add SubTopicId column to Stories (nullable, FK)
-- Add SubTopicId column to Stories (nullable, FK) only if Stories table exists
IF OBJECT_ID('dbo.Stories', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'SubTopicId' AND Object_ID = Object_ID(N'dbo.Stories'))
    BEGIN
        ALTER TABLE dbo.Stories ADD SubTopicId UNIQUEIDENTIFIER NULL;
    END

    -- Add FK only if SubTopics table exists and FK not already present
    IF OBJECT_ID('dbo.SubTopics', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Stories_SubTopics_SubTopicId')
    BEGIN
        ALTER TABLE dbo.Stories
            ADD CONSTRAINT FK_Stories_SubTopics_SubTopicId FOREIGN KEY (SubTopicId) REFERENCES dbo.SubTopics(SubTopicId) ON DELETE SET NULL;
    END
END
