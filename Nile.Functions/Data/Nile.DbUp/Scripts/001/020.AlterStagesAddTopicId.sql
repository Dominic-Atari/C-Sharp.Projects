-- Migration: Add TopicId to Stages and backfill where possible
PRINT 'Applying 020.AlterStagesAddTopicId.sql - add TopicId to Stages';

IF COL_LENGTH('dbo.Stages', 'TopicId') IS NULL
BEGIN
    ALTER TABLE dbo.Stages
    ADD TopicId UNIQUEIDENTIFIER NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Stages_TopicId')
        CREATE NONCLUSTERED INDEX IX_Stages_TopicId ON dbo.Stages (TopicId);

    PRINT 'Added TopicId to dbo.Stages';
END
ELSE
BEGIN
    PRINT 'TopicId already exists on dbo.Stages; skipping.';
END

-- Backfill suggestion: attempt to match Stage.Name to Topic.Name (case-insensitive) per school
-- WARNING: this is a best-effort backfill. Review results before committing to production use.

PRINT 'Backfilling Stage.TopicId by matching Stage.Name to Topic.TopicName (best-effort)';

-- Use runtime-executed dynamic SQL to avoid compile-time errors when referencing newly-added columns
-- and be defensive about the existence of the expected columns and tables.
IF OBJECT_ID('dbo.Topics','U') IS NOT NULL AND COL_LENGTH('dbo.Topics', 'TopicName') IS NOT NULL AND COL_LENGTH('dbo.Stages', 'TopicId') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
    UPDATE s
    SET s.TopicId = t.TopicId
    FROM dbo.Stages s
    JOIN dbo.Topics t ON t.SchoolId = s.SchoolId AND LOWER(LTRIM(RTRIM(t.TopicName))) = LOWER(LTRIM(RTRIM(s.Name)))
    WHERE s.TopicId IS NULL;';

    EXEC sp_executesql @sql;
    PRINT 'Backfill complete (review results to ensure correct matches).';
END
ELSE
BEGIN
    PRINT 'Skipping backfill: required columns or tables not present.';
END
