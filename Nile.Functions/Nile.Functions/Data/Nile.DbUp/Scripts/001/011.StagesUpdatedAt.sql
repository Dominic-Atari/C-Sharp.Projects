-- Migration: Add UpdatedAt column to Stages and set defaults
PRINT 'Applying 011.StagesUpdatedAt.sql - ensure UpdatedAt column exists on Stages...';

IF OBJECT_ID('dbo.Stages', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Stages', 'UpdatedAt') IS NULL
    BEGIN
        ALTER TABLE dbo.Stages ADD UpdatedAt DATETIME2 NULL;
        PRINT 'Added UpdatedAt column as NULL to dbo.Stages.';
        -- Use dynamic SQL for subsequent statements to avoid parse-time errors when referencing the new column
        IF COL_LENGTH('dbo.Stages', 'CreatedAt') IS NOT NULL
        BEGIN
            EXEC sp_executesql N'UPDATE dbo.Stages SET UpdatedAt = COALESCE(UpdatedAt, CreatedAt) WHERE UpdatedAt IS NULL;';
        END
        ELSE
        BEGIN
            EXEC sp_executesql N'UPDATE dbo.Stages SET UpdatedAt = SYSUTCDATETIME() WHERE UpdatedAt IS NULL;';
        END
        EXEC sp_executesql N'ALTER TABLE dbo.Stages ALTER COLUMN UpdatedAt DATETIME2 NOT NULL;';
        PRINT 'Altered UpdatedAt to NOT NULL.';
    END
    ELSE
    BEGIN
        PRINT 'UpdatedAt column already exists on dbo.Stages.';
    END

    -- Add default constraint if it's missing
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE OBJECT_NAME(dc.parent_object_id) = 'Stages' AND c.name = 'UpdatedAt')
    BEGIN
        ALTER TABLE dbo.Stages ADD CONSTRAINT DF_Stages_UpdatedAt DEFAULT SYSUTCDATETIME() FOR UpdatedAt;
    END
END
ELSE
BEGIN
    PRINT 'dbo.Stages table does not exist; skipping 011.StagesUpdatedAt.sql.';
END
