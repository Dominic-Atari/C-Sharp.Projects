-- Migration: Ensure FK constraints for SubLevels and SubLevelId columns reference correctly
PRINT 'Applying 014.AddSubLevelFKs.sql - add missing FK constraints for SubLevels and related columns';

-- If SubLevels and Stages exist, ensure the FK exists
IF OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL AND OBJECT_ID('dbo.Stages', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SubLevels_Stage')
    BEGIN
        ALTER TABLE dbo.SubLevels ADD CONSTRAINT FK_SubLevels_Stage FOREIGN KEY (StageId) REFERENCES dbo.Stages(StageId);
        PRINT 'Added FK_SubLevels_Stage';
    END
    ELSE
    BEGIN
        PRINT 'FK_SubLevels_Stage already exists';
    END
END
ELSE
BEGIN
    PRINT 'Either dbo.SubLevels or dbo.Stages missing; skipping FK_SubLevels_Stage addition.';
END

-- Add FK on SchoolMemberships.SubLevelId -> SubLevels(SubLevelId) if both columns/tables exist
IF OBJECT_ID('dbo.SchoolMemberships', 'U') IS NOT NULL AND OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SchoolMemberships') AND name = 'SubLevelId')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SchoolMemberships_SubLevel')
        BEGIN
            ALTER TABLE dbo.SchoolMemberships ADD CONSTRAINT FK_SchoolMemberships_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
            PRINT 'Added FK_SchoolMemberships_SubLevel';
        END
        ELSE
        BEGIN
            PRINT 'FK_SchoolMemberships_SubLevel already exists';
        END
    END
    ELSE
    BEGIN
        PRINT 'Column SubLevelId not present on dbo.SchoolMemberships; skipping.';
    END
END

-- Add FK on TeacherSubjects.SubLevelId -> SubLevels(SubLevelId) if both columns/tables exist
IF OBJECT_ID('dbo.TeacherSubjects', 'U') IS NOT NULL AND OBJECT_ID('dbo.SubLevels', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TeacherSubjects') AND name = 'SubLevelId')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherSubjects_SubLevel')
        BEGIN
            ALTER TABLE dbo.TeacherSubjects ADD CONSTRAINT FK_TeacherSubjects_SubLevel FOREIGN KEY (SubLevelId) REFERENCES dbo.SubLevels(SubLevelId);
            PRINT 'Added FK_TeacherSubjects_SubLevel';
        END
        ELSE
        BEGIN
            PRINT 'FK_TeacherSubjects_SubLevel already exists';
        END
    END
    ELSE
    BEGIN
        PRINT 'Column SubLevelId not present on dbo.TeacherSubjects; skipping.';
    END
END
