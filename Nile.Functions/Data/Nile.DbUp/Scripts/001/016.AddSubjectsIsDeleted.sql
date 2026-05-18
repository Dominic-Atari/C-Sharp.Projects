-- Add IsDeleted and DeletedAt to dbo.Subjects if missing (mirror for alternate DbUp folder)
PRINT 'Applying 016.AddSubjectsIsDeleted.sql - add IsDeleted/DeletedAt to Subjects if missing';

IF OBJECT_ID('dbo.Subjects','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Subjects','IsDeleted') IS NULL
    BEGIN
        ALTER TABLE dbo.Subjects ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Subjects_IsDeleted DEFAULT(0);
        PRINT 'Added IsDeleted column to dbo.Subjects';
    END
    ELSE
    BEGIN
        PRINT 'dbo.Subjects.IsDeleted already exists; skipping';
    END

    IF COL_LENGTH('dbo.Subjects','DeletedAt') IS NULL
    BEGIN
        ALTER TABLE dbo.Subjects ADD DeletedAt DATETIME2 NULL;
        PRINT 'Added DeletedAt column to dbo.Subjects';
    END
    ELSE
    BEGIN
        PRINT 'dbo.Subjects.DeletedAt already exists; skipping';
    END
END
ELSE
BEGIN
    PRINT 'dbo.Subjects does not exist; skipping migration 016.AddSubjectsIsDeleted.sql';
END
