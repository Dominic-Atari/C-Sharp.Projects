-- 012.AlterSubjectSchoolId_DropDependentIndexes.sql
-- Safely alter Subjects.SchoolId to NOT NULL by dropping non-PK indexes that reference SchoolId,
-- altering the column, then recreating the expected indexes and FK.
PRINT 'Running 012.AlterSubjectSchoolId_DropDependentIndexes.sql';

-- Back up: list indexes that reference SchoolId (non-PK)
IF OBJECT_ID('tempdb..#ix_schoolid') IS NOT NULL DROP TABLE #ix_schoolid;
CREATE TABLE #ix_schoolid (IxName sysname);

INSERT INTO #ix_schoolid (IxName)
SELECT DISTINCT ix.name
FROM sys.indexes ix
JOIN sys.index_columns ic ON ix.object_id = ic.object_id AND ix.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE ix.object_id = OBJECT_ID('dbo.Subjects')
  AND c.name = 'SchoolId'
  AND ix.is_primary_key = 0
  AND ix.is_unique_constraint = 0;

DECLARE @ix NVARCHAR(200);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT IxName FROM #ix_schoolid;
OPEN cur; FETCH NEXT FROM cur INTO @ix;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping index: ' + @ix;
    EXEC('IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''' + @ix + ''' AND object_id = OBJECT_ID(''dbo.Subjects'')) DROP INDEX [' + @ix + '] ON dbo.Subjects;');
    FETCH NEXT FROM cur INTO @ix;
END
CLOSE cur; DEALLOCATE cur;

-- Alter column to NOT NULL if possible
BEGIN TRY
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Subjects') AND name = 'SchoolId')
    BEGIN
        -- Only alter if there are no NULLs remaining or if we can set NOT NULL safely
        IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SchoolId IS NULL)
        BEGIN
            ALTER TABLE dbo.Subjects ALTER COLUMN SchoolId UNIQUEIDENTIFIER NOT NULL;
            PRINT 'Altered Subjects.SchoolId to NOT NULL.';
        END
        ELSE
        BEGIN
            PRINT 'Subjects.SchoolId contains NULLs; skipping ALTER to NOT NULL. Populate NULLs first.';
        END
    END
END TRY
BEGIN CATCH
    PRINT 'Alter failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

-- Recreate expected unique index on (SchoolId, Name, Stage)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Subjects_School_Name_Stage' AND object_id = OBJECT_ID('dbo.Subjects'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Subjects_School_Name_Stage ON dbo.Subjects (SchoolId, Name, Stage);
    PRINT 'Created IX_Subjects_School_Name_Stage';
END

-- Ensure FK exists
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subjects_Schools')
BEGIN
    ALTER TABLE dbo.Subjects ADD CONSTRAINT FK_Subjects_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId) ON DELETE CASCADE;
    PRINT 'Added FK_Subjects_Schools';
END

PRINT '012 script completed.';
