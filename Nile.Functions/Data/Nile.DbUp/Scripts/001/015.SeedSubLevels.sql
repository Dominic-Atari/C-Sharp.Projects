-- Migration: Seed default SubLevels for stages where none exist
PRINT 'Applying 015.SeedSubLevels.sql - add default sublevels for stages';

IF OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL AND OBJECT_ID('dbo.Stages', 'U') IS NOT NULL
BEGIN
    -- Insert a default sublevel for each stage that has none (idempotent)
    INSERT INTO dbo.SubLevels (SubLevelId, StageId, [Name], [Label], [Description], CreatedAt, UpdatedAt, IsDeleted)
    SELECT NEWID(), s.StageId, 'Default ' + s.Name, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME(), 0
    FROM dbo.Stages s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubLevels sl WHERE sl.StageId = s.StageId AND sl.IsDeleted = 0);

    PRINT 'Inserted default sublevels for stages where missing.';
END
ELSE
BEGIN
    PRINT 'dbo.SubLevels or dbo.Stages does not exist; skipping seeding.';
END
