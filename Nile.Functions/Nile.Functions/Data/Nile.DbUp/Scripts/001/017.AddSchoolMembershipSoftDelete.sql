-- Migration: add IsDeleted and DeletedAt columns to SchoolMemberships for soft-delete
PRINT 'Applying 017.AddSchoolMembershipSoftDelete.sql - add soft-delete columns to SchoolMemberships';

IF OBJECT_ID('dbo.SchoolMemberships', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SchoolMemberships') AND name = 'IsDeleted')
    BEGIN
        ALTER TABLE dbo.SchoolMemberships ADD IsDeleted bit NOT NULL DEFAULT 0;
        PRINT 'Added column IsDeleted to dbo.SchoolMemberships';
    END
    ELSE
        PRINT 'Column IsDeleted already exists on dbo.SchoolMemberships';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SchoolMemberships') AND name = 'DeletedAt')
    BEGIN
        ALTER TABLE dbo.SchoolMemberships ADD DeletedAt datetime2 NULL;
        PRINT 'Added column DeletedAt to dbo.SchoolMemberships';
    END
    ELSE
        PRINT 'Column DeletedAt already exists on dbo.SchoolMemberships';
END
ELSE
    PRINT 'Table dbo.SchoolMemberships does not exist; skipping soft-delete migration';
