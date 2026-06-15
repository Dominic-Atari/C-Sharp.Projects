-- 009.AddSubjectSchoolId.sql
-- Adds SchoolId column to Subjects and links it to Schools. Idempotent.
PRINT 'Applying AddSubjectSchoolId migration...';

-- Drop any non-PK, non-unique-constraint indexes that reference SchoolId to allow ALTER COLUMN
IF OBJECT_ID('tempdb..#ix_schoolid_temp') IS NOT NULL DROP TABLE #ix_schoolid_temp;
CREATE TABLE #ix_schoolid_temp (IxName sysname);

INSERT INTO #ix_schoolid_temp (IxName)
SELECT DISTINCT ix.name
FROM sys.indexes ix
JOIN sys.index_columns ic ON ix.object_id = ic.object_id AND ix.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE ix.object_id = OBJECT_ID('dbo.Subjects')
  AND c.name = 'SchoolId'
  AND ix.is_primary_key = 0
  AND ix.is_unique_constraint = 0;

DECLARE @ixTemp NVARCHAR(200);
DECLARE curTemp CURSOR LOCAL FAST_FORWARD FOR SELECT IxName FROM #ix_schoolid_temp;
OPEN curTemp; FETCH NEXT FROM curTemp INTO @ixTemp;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping index (temp): ' + @ixTemp;
    EXEC('IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''' + @ixTemp + ''' AND object_id = OBJECT_ID(''dbo.Subjects'')) DROP INDEX [' + @ixTemp + '] ON dbo.Subjects;');
    FETCH NEXT FROM curTemp INTO @ixTemp;
END
CLOSE curTemp; DEALLOCATE curTemp;

IF COL_LENGTH('dbo.Subjects', 'SchoolId') IS NULL
BEGIN
    ALTER TABLE dbo.Subjects ADD SchoolId UNIQUEIDENTIFIER NULL;
END

-- If there are existing Schools, set any NULL Subject SchoolId to the first SchoolId (best-effort)
IF EXISTS (SELECT 1 FROM dbo.Schools)
BEGIN
    DECLARE @defaultSchoolId UNIQUEIDENTIFIER = (SELECT TOP 1 SchoolId FROM dbo.Schools ORDER BY CreatedAt ASC);
    UPDATE dbo.Subjects SET SchoolId = @defaultSchoolId WHERE SchoolId IS NULL;
END

-- Make SchoolId NOT NULL only if there are no NULLs left
IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SchoolId IS NULL)
BEGIN
    EXEC('ALTER TABLE dbo.Subjects ALTER COLUMN SchoolId UNIQUEIDENTIFIER NOT NULL;');
END

-- Add foreign key to Schools
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subjects_Schools')
    ALTER TABLE dbo.Subjects ADD CONSTRAINT FK_Subjects_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId) ON DELETE CASCADE;

-- Ensure unique index (SchoolId, Name, Stage) exists
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Subjects_School_Name_Stage')
    CREATE UNIQUE NONCLUSTERED INDEX IX_Subjects_School_Name_Stage ON dbo.Subjects (SchoolId, Name, Stage);

PRINT 'AddSubjectSchoolId migration completed.';
