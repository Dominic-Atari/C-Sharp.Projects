-- Idempotent script: create SubLevels table, add columns/fks, and seed defaults

-- 1) Create SubLevels table if missing
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
    PRINT 'Created dbo.SubLevels';
END
ELSE
BEGIN
    PRINT 'dbo.SubLevels already exists; skipping create';
END

-- 2) Ensure SubLevelId columns exist on SchoolMemberships and TeacherSubjects
IF OBJECT_ID('dbo.SchoolMemberships', 'U') IS NOT NULL AND COL_LENGTH('dbo.SchoolMemberships','SubLevelId') IS NULL
BEGIN
    ALTER TABLE dbo.SchoolMemberships ADD SubLevelId UNIQUEIDENTIFIER NULL;
    PRINT 'Added SubLevelId to dbo.SchoolMemberships';
END

IF OBJECT_ID('dbo.TeacherSubjects', 'U') IS NOT NULL AND COL_LENGTH('dbo.TeacherSubjects','SubLevelId') IS NULL
BEGIN
    ALTER TABLE dbo.TeacherSubjects ADD SubLevelId UNIQUEIDENTIFIER NULL;
    PRINT 'Added SubLevelId to dbo.TeacherSubjects';
END

-- 3) Add FK constraints only if referenced tables/columns exist and FKs don't already exist
IF OBJECT_ID('dbo.SubLevels','U') IS NOT NULL AND OBJECT_ID('dbo.Stages','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SubLevels_Stage')
        ALTER TABLE dbo.SubLevels ADD CONSTRAINT FK_SubLevels_Stage FOREIGN KEY (StageId) REFERENCES dbo.Stages(StageId);
END

IF OBJECT_ID('dbo.SchoolMemberships','U') IS NOT NULL AND OBJECT_ID('dbo.SubLevels','U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SchoolMemberships') AND name = 'SubLevelId')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SchoolMemberships_SubLevel')
            ALTER TABLE dbo.SchoolMemberships ADD CONSTRAINT FK_SchoolMemberships_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
    END
END

IF OBJECT_ID('dbo.TeacherSubjects','U') IS NOT NULL AND OBJECT_ID('dbo.SubLevels','U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TeacherSubjects') AND name = 'SubLevelId')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherSubjects_SubLevel')
            ALTER TABLE dbo.TeacherSubjects ADD CONSTRAINT FK_TeacherSubjects_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
    END
END

-- 4) Seed default SubLevels for any Stage that has no active sublevel (idempotent)
IF OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL AND OBJECT_ID('dbo.Stages', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.SubLevels (SubLevelId, StageId, [Name], [Label], [Description], CreatedAt, UpdatedAt, IsDeleted)
    SELECT NEWID(), s.StageId, 'Default ' + s.Name, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME(), 0
    FROM dbo.Stages s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SubLevels sl WHERE sl.StageId = s.StageId AND sl.IsDeleted = 0
    );

    PRINT 'Seed complete: default sublevels inserted (if any were missing).';
END
ELSE
BEGIN
    PRINT 'Skipping seeding: dbo.SubLevels or dbo.Stages not present.';
END