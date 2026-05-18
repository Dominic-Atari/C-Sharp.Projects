-- 021.CleanupParentTopicIds.sql
-- Purpose: Normalize and remove invalid ParentTopicId references to prevent mis-scoped subtopics
-- Safe to run multiple times (idempotent updates where possible). Run in test/dev first.

SET NOCOUNT ON;
-- Guard: only run when the Topics table exists and the ParentTopicId column exists (avoids errors on older DBs)
IF OBJECT_ID('dbo.Topics', 'U') IS NOT NULL AND COL_LENGTH('dbo.Topics','ParentTopicId') IS NOT NULL
BEGIN
    -- Use dynamic SQL to avoid compile-time name resolution errors when the column is absent in some DB states.
    DECLARE @sql NVARCHAR(MAX) = N'';
    SET @sql = @sql + N'BEGIN TRY\n    BEGIN TRANSACTION;\n\n    -- 1) Null-out self-referential ParentTopicId (Topic where ParentTopicId == TopicId)\n    UPDATE dbo.Topics\n    SET ParentTopicId = NULL, UpdatedAt = SYSUTCDATETIME()\n    WHERE ParentTopicId IS NOT NULL AND ParentTopicId = TopicId;\n\n    -- 2) Null-out ParentTopicId references to missing topics (orphaned parent references)\n    UPDATE t\n    SET ParentTopicId = NULL, UpdatedAt = SYSUTCDATETIME()\n    FROM dbo.Topics t\n    LEFT JOIN dbo.Topics p ON t.ParentTopicId = p.TopicId\n    WHERE t.ParentTopicId IS NOT NULL AND p.TopicId IS NULL;\n\n    -- 3) Null-out parents that belong to a different school or subject\n    UPDATE t\n    SET ParentTopicId = NULL, UpdatedAt = SYSUTCDATETIME()\n    FROM dbo.Topics t\n    JOIN dbo.Topics p ON t.ParentTopicId = p.TopicId\n    WHERE t.ParentTopicId IS NOT NULL AND (t.SchoolId <> p.SchoolId OR t.SubjectId <> p.SubjectId);\n\n    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;';

    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    PRINT 'Skipping 021.CleanupParentTopicIds.sql — table dbo.Topics not found or ParentTopicId missing.';
END;

-- Verification queries (run manually after this migration if you need quick checks)
-- SELECT COUNT(*) AS TotalTopics, SUM(CASE WHEN ParentTopicId IS NULL THEN 1 ELSE 0 END) AS ParentNullCount FROM dbo.Topics;
-- SELECT TOP 100 TopicId, ParentTopicId, Subtopic, TopicName FROM dbo.Topics WHERE ParentTopicId IS NOT NULL ORDER BY ParentTopicId;

-- Notes:
-- - This migration is intentionally conservative: it nulls invalid parent pointers rather than guessing a parent
--   to avoid moving children under an unintended parent.
-- - If you want to re-associate child rows to a different parent, do so with a well-tested backfill script.
-- - Run this on dev/test first and validate the UI and data before applying to production.
