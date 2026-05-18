-- Migration: Create Stages table (levels) if missing
PRINT 'Applying 014.CreateStages.sql - create Stages table if missing';

IF OBJECT_ID('dbo.Stages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stages (
        StageId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Stages PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Label] NVARCHAR(200) NULL,
        [Description] NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Stages_IsDeleted DEFAULT (0),
        DeletedAt DATETIME2 NULL
    );

    PRINT 'Created table dbo.Stages';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Stages_SchoolId_Name')
        CREATE UNIQUE NONCLUSTERED INDEX IX_Stages_SchoolId_Name ON dbo.Stages (SchoolId, [Name]);

    IF OBJECT_ID('dbo.Schools','U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Stages_Schools')
            ALTER TABLE dbo.Stages ADD CONSTRAINT FK_Stages_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId) ON DELETE CASCADE;
    END
END
ELSE
BEGIN
    PRINT 'dbo.Stages already exists; skipping create.';
END
