-- 023.MakeTopicSubtopicNullable.sql
-- Purpose: Allow NULLs in dbo.Topics.Subtopic because subtopics are now stored in dbo.SubTopics.
-- Idempotent: Only alters column when it is currently NOT NULL.

IF OBJECT_ID('dbo.Topics', 'U') IS NULL
BEGIN
    PRINT '023: Skipping — dbo.Topics table not found.';
    RETURN;
END

DECLARE @isNullable nvarchar(3);
SELECT @isNullable = IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Topics' AND COLUMN_NAME = 'Subtopic';

IF @isNullable = 'NO'
BEGIN
    PRINT '023: Altering dbo.Topics.Subtopic to NVARCHAR(450) NULL';
    -- Match existing length (seen in parameter size 450) and change to NULL
    ALTER TABLE dbo.Topics ALTER COLUMN [Subtopic] NVARCHAR(450) NULL;
END
ELSE
BEGIN
    PRINT '023: dbo.Topics.Subtopic already nullable — skipping';
END

PRINT '023: MakeTopicSubtopicNullable completed.';
