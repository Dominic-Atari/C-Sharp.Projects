-- 1) Backup topics (idempotent)
-- Only proceed if Topics table exists
IF OBJECT_ID('dbo.Topics') IS NULL
BEGIN
  PRINT 'Skipping cleanup script: dbo.Topics not found.';
  RETURN;
END

IF OBJECT_ID('dbo.Topics_Backup_20251225') IS NULL
BEGIN
  SELECT TOP (0) * INTO dbo.Topics_Backup_20251225 FROM dbo.Topics; -- create structure
END
INSERT INTO dbo.Topics_Backup_20251225
SELECT * FROM dbo.Topics; -- snapshot

-- 2) Start safe transaction
BEGIN TRANSACTION;

-- 3) Dynamically delete from child tables that reference Topics
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = STRING_AGG('DELETE FROM ' + QUOTENAME(s.name) + '.' + QUOTENAME(rt.name) +
                         ' WHERE ' + QUOTENAME(rc.name) + ' IN (SELECT TopicId FROM dbo.Topics);', CHAR(13)+CHAR(10))
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables rt ON fkc.parent_object_id = rt.object_id
JOIN sys.schemas s ON rt.schema_id = s.schema_id
JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.parent_column_id
JOIN sys.tables t ON fkc.referenced_object_id = t.object_id
JOIN sys.schemas rs ON t.schema_id = rs.schema_id
WHERE t.name = 'Topics' AND rs.name = 'dbo';

-- If @sql is not null, execute child deletes
IF @sql IS NOT NULL AND LEN(@sql) > 0
BEGIN
  PRINT 'Deleting rows from child tables referencing Topics...';
  EXEC sp_executesql @sql;
END
ELSE
BEGIN
  PRINT 'No FK children found. Proceeding to delete/truncate Topics.';
END

-- 4) Delete topics (safe, works with FK deletions performed)
DELETE FROM dbo.Topics;

-- 5) Optionally reseed identity (uncomment if Topics has identity PK)
-- DBCC CHECKIDENT('dbo.Topics', RESEED, 0);

-- 6) Verify and commit
SELECT COUNT(*) AS RemainingTopics FROM dbo.Topics;
-- If everything looks good:
COMMIT TRANSACTION;
-- If something is wrong, run:
-- ROLLBACK TRANSACTION;