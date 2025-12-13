-- Migration: Enforce soft-delete-only strategy, seed defaults, and utilities
-- Safe to run on existing databases (idempotent where possible)

------------------------------------------------------------
-- 1) Recreate FKs without ON DELETE CASCADE
------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Schools_Users')
BEGIN
    ALTER TABLE dbo.Schools DROP CONSTRAINT FK_Schools_Users;
    ALTER TABLE dbo.Schools
        ADD CONSTRAINT FK_Schools_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Passwords_Users')
BEGIN
    ALTER TABLE dbo.Passwords DROP CONSTRAINT FK_Passwords_Users;
    ALTER TABLE dbo.Passwords
        ADD CONSTRAINT FK_Passwords_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmailConfirmations_Users')
BEGIN
    ALTER TABLE dbo.EmailConfirmations DROP CONSTRAINT FK_EmailConfirmations_Users;
    ALTER TABLE dbo.EmailConfirmations
        ADD CONSTRAINT FK_EmailConfirmations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResetPasswordTokens_Users')
BEGIN
    ALTER TABLE dbo.ResetPasswordTokens DROP CONSTRAINT FK_ResetPasswordTokens_Users;
    ALTER TABLE dbo.ResetPasswordTokens
        ADD CONSTRAINT FK_ResetPasswordTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRole_User')
BEGIN
    ALTER TABLE dbo.UserRole DROP CONSTRAINT FK_UserRole_User;
    ALTER TABLE dbo.UserRole
        ADD CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRole_Role')
BEGIN
    ALTER TABLE dbo.UserRole DROP CONSTRAINT FK_UserRole_Role;
    ALTER TABLE dbo.UserRole
        ADD CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId);
END
GO

------------------------------------------------------------
-- 2) Ensure RelationshipType is INT NOT NULL (valid SQL Server type)
-- Drop/recreate dependent unique constraint only if change is needed
------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.UserRelationships')
      AND c.name = 'RelationshipType'
      AND (t.name <> 'int')
)
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE name = 'UQ_UserRelationships'
          AND parent_object_id = OBJECT_ID('dbo.UserRelationships')
    )
        ALTER TABLE dbo.UserRelationships DROP CONSTRAINT UQ_UserRelationships;

    ALTER TABLE dbo.UserRelationships
        ALTER COLUMN RelationshipType INT NOT NULL;

    ALTER TABLE dbo.UserRelationships
        ADD CONSTRAINT UQ_UserRelationships UNIQUE (FollowerUserId, FollowingUserId, RelationshipType);
END
ELSE
BEGIN
    -- If type is already INT, ensure NOT NULL if needed (drop/recreate unique constraint as required)
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.UserRelationships')
          AND name = 'RelationshipType'
          AND is_nullable = 1
    )
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.key_constraints
            WHERE name = 'UQ_UserRelationships'
              AND parent_object_id = OBJECT_ID('dbo.UserRelationships')
        )
            ALTER TABLE dbo.UserRelationships DROP CONSTRAINT UQ_UserRelationships;

        ALTER TABLE dbo.UserRelationships
            ALTER COLUMN RelationshipType INT NOT NULL;

        ALTER TABLE dbo.UserRelationships
            ADD CONSTRAINT UQ_UserRelationships UNIQUE (FollowerUserId, FollowingUserId, RelationshipType);
    END
END
GO

------------------------------------------------------------
-- 3) Recreate IX_Posts_CreatedAt without DESC (consistency)
------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Posts_CreatedAt' AND object_id = OBJECT_ID('dbo.Posts'))
BEGIN
    DROP INDEX IX_Posts_CreatedAt ON dbo.Posts;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Posts_CreatedAt' AND object_id = OBJECT_ID('dbo.Posts'))
BEGIN
    CREATE INDEX IX_Posts_CreatedAt ON dbo.Posts(CreatedAt);
END
GO

------------------------------------------------------------
-- 4) Add soft-delete columns where missing
------------------------------------------------------------
IF COL_LENGTH('dbo.Likes', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Likes
        ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Likes_IsDeleted DEFAULT (0) WITH VALUES,
            DeletedAt DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.UserRelationships', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.UserRelationships
        ADD IsDeleted BIT NOT NULL CONSTRAINT DF_UserRelationships_IsDeleted DEFAULT (0) WITH VALUES,
            DeletedAt DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.Notifications', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications
        ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Notifications_IsDeleted DEFAULT (0) WITH VALUES,
            DeletedAt DATETIME2 NULL;
END
GO

------------------------------------------------------------
-- 5) Seed default roles (idempotent)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = 'User')
BEGIN
    INSERT INTO dbo.Role (RoleId, RoleName) VALUES (NEWID(), 'User');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO dbo.Role (RoleId, RoleName) VALUES (NEWID(), 'Admin');
END
GO

------------------------------------------------------------
-- 6) Stored Procedure: Soft delete a user and related data
------------------------------------------------------------
IF OBJECT_ID('dbo.SoftDeleteUser', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SoftDeleteUser AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.SoftDeleteUser
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @now DATETIME2 = SYSUTCDATETIME();

    -- Users table no longer carries soft-delete columns; keep user row as-is.

    -- Owned entities
    UPDATE dbo.Schools
    SET IsDeleted = 1, DeletedAt = @now, UpdatedAt = @now
    WHERE UserId = @UserId AND IsDeleted = 0;

    UPDATE dbo.Posts
    SET IsDeleted = 1, DeletedAt = @now, UpdatedAt = @now
    WHERE UserId = @UserId AND IsDeleted = 0;

    UPDATE dbo.Comments
    SET IsDeleted = 1, DeletedAt = @now, UpdatedAt = @now
    WHERE UserId = @UserId AND IsDeleted = 0;

    UPDATE dbo.Likes
    SET IsDeleted = 1, DeletedAt = @now
    WHERE UserId = @UserId AND IsDeleted = 0;

    UPDATE dbo.UserRelationships
    SET IsDeleted = 1, DeletedAt = @now
    WHERE (FollowerUserId = @UserId OR FollowingUserId = @UserId) AND IsDeleted = 0;

    UPDATE dbo.Notifications
    SET IsDeleted = 1, DeletedAt = @now
    WHERE UserId = @UserId AND IsDeleted = 0;
END
GO

PRINT 'Soft-delete-only migration applied successfully.';
