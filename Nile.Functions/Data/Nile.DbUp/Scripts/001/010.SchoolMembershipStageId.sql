-- Add StageId to SchoolMemberships to support student stage assignment
PRINT 'Adding StageId to dbo.SchoolMemberships if missing...';

IF OBJECT_ID('dbo.SchoolMemberships', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SchoolMemberships', 'StageId') IS NULL
    BEGIN
        ALTER TABLE dbo.SchoolMemberships ADD StageId UNIQUEIDENTIFIER NULL;
        PRINT 'Added StageId column to dbo.SchoolMemberships.';
    END
    ELSE
        PRINT 'StageId column already exists on dbo.SchoolMemberships.';
END
ELSE
BEGIN
    PRINT 'dbo.SchoolMemberships table does not exist; skipping StageId addition.';
END
