-- Migration: Add ClientCorrelationId to Stories
PRINT 'Applying 019.AlterStoriesAddClientCorrelationId.sql - add ClientCorrelationId to Stories';

IF COL_LENGTH('dbo.Stories', 'ClientCorrelationId') IS NULL
BEGIN
    ALTER TABLE dbo.Stories
    ADD ClientCorrelationId NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Stories_ClientCorrelationId')
        CREATE NONCLUSTERED INDEX IX_Stories_ClientCorrelationId ON dbo.Stories (ClientCorrelationId);

    PRINT 'Added ClientCorrelationId to dbo.Stories';
END
ELSE
BEGIN
    PRINT 'ClientCorrelationId already exists on dbo.Stories; skipping.';
END
