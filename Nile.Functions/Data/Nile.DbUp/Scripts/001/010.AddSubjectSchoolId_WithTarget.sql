-- 010.AddSubjectSchoolId_WithTarget.sql
-- Adds SchoolId to Subjects; accepts optional SQLCMD variable TargetSchoolId
-- Usage (sqlcmd):
-- sqlcmd -S <server> -d <db> -U <user> -P <pw> -i 010.AddSubjectSchoolId_WithTarget.sql -v TargetSchoolId="00000000-0000-0000-0000-000000000000"
-- :setvar TargetSchoolId ""
PRINT 'Applying AddSubjectSchoolId (with optional TargetSchoolId) migration...';

DECLARE @targetVar NVARCHAR(100) = '$(TargetSchoolId)';
DECLARE @targetGuid UNIQUEIDENTIFIER = NULL;
-- If the script was run without SQLCMD substitution the literal '$(TargetSchoolId)'
-- will be present. Treat that as "not provided" and fall back to default behavior.
IF LTRIM(RTRIM(@targetVar)) <> '' AND @targetVar NOT LIKE '$(TargetSchoolId)'
BEGIN
    SET @targetGuid = TRY_CAST(@targetVar AS UNIQUEIDENTIFIER);
    IF @targetGuid IS NULL
    BEGIN
        RAISERROR('TargetSchoolId provided is not a valid GUID: %s', 16, 1, @targetVar);
        RETURN;
    END
END

-- Ensure column exists
IF COL_LENGTH('dbo.Subjects', 'SchoolId') IS NULL
BEGIN
    ALTER TABLE dbo.Subjects ADD SchoolId UNIQUEIDENTIFIER NULL;
END

IF @targetGuid IS NOT NULL
BEGIN
    PRINT 'Using provided TargetSchoolId to populate existing subjects.';
    UPDATE dbo.Subjects SET SchoolId = @targetGuid WHERE SchoolId IS NULL;
END
ELSE
BEGIN
    PRINT 'No TargetSchoolId provided. Falling back to first school (best-effort) if any.';
    IF EXISTS (SELECT 1 FROM dbo.Schools)
    BEGIN
        DECLARE @defaultSchoolId UNIQUEIDENTIFIER = (SELECT TOP 1 SchoolId FROM dbo.Schools ORDER BY CreatedAt ASC);
        UPDATE dbo.Subjects SET SchoolId = @defaultSchoolId WHERE SchoolId IS NULL;
    END
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

PRINT 'AddSubjectSchoolId (with Target) migration completed.';
