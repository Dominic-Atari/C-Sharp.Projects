-- Migration: Add soft-delete columns to Stages
-- Idempotent - safe to run on existing DBs

IF OBJECT_ID('dbo.Stages', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Stages', 'IsDeleted') IS NULL
    BEGIN
        ALTER TABLE dbo.Stages
            ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Stages_IsDeleted DEFAULT (0) WITH VALUES,
                DeletedAt DATETIME2 NULL;
    END

    PRINT 'Applied migration 009.StagesSoftDelete.sql - added IsDeleted/DeletedAt to Stages (if missing)';
END
ELSE
BEGIN
    PRINT 'Stages table not present; skipping 009.StagesSoftDelete.sql';
END
