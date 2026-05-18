-- Migration: Create SubLevels table and add SubLevelId foreign keys
PRINT 'Applying 012.SubLevels.sql - add SubLevels table and SubLevelId columns';

IF OBJECT_ID('dbo.SubLevels', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubLevels (
        SubLevelId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        StageId UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Label] NVARCHAR(200) NULL,
        [Description] NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_SubLevels_IsDeleted DEFAULT (0),
        DeletedAt DATETIME2 NULL
    );

    PRINT 'Created table dbo.SubLevels';

    CREATE UNIQUE INDEX IX_SubLevels_StageId_Name ON dbo.SubLevels(StageId, [Name]);
END
ELSE
BEGIN
    PRINT 'dbo.SubLevels already exists; skipping create.';
END

-- Add SubLevelId to SchoolMemberships
IF OBJECT_ID('dbo.SchoolMemberships', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SchoolMemberships', 'SubLevelId') IS NULL
    BEGIN
        ALTER TABLE dbo.SchoolMemberships ADD SubLevelId UNIQUEIDENTIFIER NULL;
        PRINT 'Added SubLevelId to dbo.SchoolMemberships';
        -- add foreign key if table exists
        IF OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL
        BEGIN
            ALTER TABLE dbo.SchoolMemberships ADD CONSTRAINT FK_SchoolMemberships_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
        END
    END
    ELSE
    BEGIN
        PRINT 'SubLevelId already exists on dbo.SchoolMemberships';
    END
END
ELSE
BEGIN
    PRINT 'dbo.SchoolMemberships does not exist; skipping SubLevelId addition.';
END

-- Add SubLevelId to TeacherSubjects
IF OBJECT_ID('dbo.TeacherSubjects', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TeacherSubjects', 'SubLevelId') IS NULL
    BEGIN
        ALTER TABLE dbo.TeacherSubjects ADD SubLevelId UNIQUEIDENTIFIER NULL;
        PRINT 'Added SubLevelId to dbo.TeacherSubjects';
        IF OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL
        BEGIN
            ALTER TABLE dbo.TeacherSubjects ADD CONSTRAINT FK_TeacherSubjects_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
        END
    END
    ELSE
    BEGIN
        PRINT 'SubLevelId already exists on dbo.TeacherSubjects';
    END
END
ELSE
BEGIN
    PRINT 'dbo.TeacherSubjects does not exist; skipping SubLevelId addition.';
END
