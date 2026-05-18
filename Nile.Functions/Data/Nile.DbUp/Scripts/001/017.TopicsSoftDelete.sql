-- Migration: Add soft-delete columns to Topics table
-- Idempotent: will only add columns if they do not already exist

-- Only attempt to alter the table if it exists and the columns are missing
IF OBJECT_ID('dbo.Topics') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Topics', 'IsDeleted') IS NULL
    BEGIN
        ALTER TABLE dbo.Topics
            ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Topics_IsDeleted DEFAULT (0) WITH VALUES,
                DeletedAt DATETIME2 NULL;
    END
END
ELSE
BEGIN
    PRINT 'Skipping Topics soft-delete migration: dbo.Topics table not found.';
END
GO

PRINT 'Topics soft-delete migration applied.';
