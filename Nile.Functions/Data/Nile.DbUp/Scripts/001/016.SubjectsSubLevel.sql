-- Migration: Add SubLevelId to Subjects and FK to SubLevels
PRINT 'Applying 016.SubjectsSubLevel.sql - add SubLevelId to Subjects and FK to SubLevels';

IF OBJECT_ID('dbo.Subjects', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Subjects', 'SubLevelId') IS NULL
    BEGIN
        ALTER TABLE dbo.Subjects ADD SubLevelId UNIQUEIDENTIFIER NULL;
        PRINT 'Added SubLevelId to dbo.Subjects';
    END
    ELSE
    BEGIN
        PRINT 'SubLevelId already exists on dbo.Subjects';
    END

    -- Add foreign key if SubLevels table exists
    IF OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Subjects_SubLevel')
        BEGIN
            ALTER TABLE dbo.Subjects ADD CONSTRAINT FK_Subjects_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
            PRINT 'Added FK_Subjects_SubLevel';
        END
        ELSE
        BEGIN
            PRINT 'FK_Subjects_SubLevel already exists';
        END
    END
    ELSE
    BEGIN
        PRINT 'dbo.SubLevels does not exist; skipping FK creation for Subjects';
    END
END
ELSE
BEGIN
    PRINT 'dbo.Subjects does not exist; skipping 016.SubjectsSubLevel.sql';
END
