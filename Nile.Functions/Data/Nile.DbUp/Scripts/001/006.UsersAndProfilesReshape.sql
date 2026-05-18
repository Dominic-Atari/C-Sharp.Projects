-- Migration: reshape Users table and introduce UserProfiles
-- Safe to run once; reruns become no-op after column rename completes.

PRINT 'Starting Users/UserProfiles reshape...';

------------------------------------------------------------
-- Guard: only run the reshape once (when UserId column exists)
------------------------------------------------------------
IF COL_LENGTH('dbo.Users', 'UserId') IS NOT NULL
BEGIN
    -- Prevent data loss by enforcing the new username length upfront
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE LEN(Username) > 15)
    BEGIN
        RAISERROR('Migration halted: one or more usernames exceed 15 characters.', 16, 1);
        RETURN;
    END

    --------------------------------------------------------
    -- Ensure UserProfiles table exists before dropping columns
    --------------------------------------------------------
    IF OBJECT_ID('dbo.UserProfiles', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UserProfiles
        (
            Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
                CONSTRAINT PK_UserProfiles PRIMARY KEY NONCLUSTERED,
            UserId    UNIQUEIDENTIFIER NOT NULL,
            FirstName NVARCHAR(24)     NOT NULL,
            LastName  NVARCHAR(24)     NOT NULL,
            Location  NVARCHAR(100)    NULL,
            Bio       NVARCHAR(1000)   NULL
        );
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserProfiles_UserId' AND object_id = OBJECT_ID('dbo.UserProfiles'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_UserProfiles_UserId ON dbo.UserProfiles (UserId);
    END

    --------------------------------------------------------
    -- Seed UserProfiles from existing Users data (idempotent)
    -- Use dynamic SQL to avoid compile-time name resolution when columns don't exist
    --------------------------------------------------------
    DECLARE @seedSql nvarchar(max) = N'
        INSERT INTO dbo.UserProfiles (Id, UserId, FirstName, LastName, Location, Bio)
        SELECT NEWID(), u.UserId,
               LEFT(ISNULL(u.FirstName, ''''), 24),
               LEFT(ISNULL(u.LastName, ''''), 24),
               CASE WHEN u.Location IS NULL THEN NULL ELSE LEFT(u.Location, 100) END,
               CASE WHEN u.Bio IS NULL THEN NULL ELSE LEFT(u.Bio, 1000) END
        FROM dbo.Users u
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.UserProfiles up WHERE up.UserId = u.UserId
        );';
    EXEC sp_executesql @seedSql;

    --------------------------------------------------------
    -- Drop foreign keys that reference Users so we can reshape the PK
    --------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Schools_Users') ALTER TABLE dbo.Schools DROP CONSTRAINT FK_Schools_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Passwords_Users') ALTER TABLE dbo.Passwords DROP CONSTRAINT FK_Passwords_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmailConfirmations_Users') ALTER TABLE dbo.EmailConfirmations DROP CONSTRAINT FK_EmailConfirmations_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResetPasswordTokens_Users') ALTER TABLE dbo.ResetPasswordTokens DROP CONSTRAINT FK_ResetPasswordTokens_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRole_User') ALTER TABLE dbo.UserRole DROP CONSTRAINT FK_UserRole_User;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Posts_Users') ALTER TABLE dbo.Posts DROP CONSTRAINT FK_Posts_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Comments_Users') ALTER TABLE dbo.Comments DROP CONSTRAINT FK_Comments_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Likes_Users') ALTER TABLE dbo.Likes DROP CONSTRAINT FK_Likes_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRelationships_Follower') ALTER TABLE dbo.UserRelationships DROP CONSTRAINT FK_UserRelationships_Follower;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRelationships_Following') ALTER TABLE dbo.UserRelationships DROP CONSTRAINT FK_UserRelationships_Following;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Users') ALTER TABLE dbo.Notifications DROP CONSTRAINT FK_Notifications_Users;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Actor') ALTER TABLE dbo.Notifications DROP CONSTRAINT FK_Notifications_Actor;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserProfiles_UserId') ALTER TABLE dbo.UserProfiles DROP CONSTRAINT FK_UserProfiles_UserId;

    --------------------------------------------------------
    -- Drop PK and unique constraints tied to old shape
    --------------------------------------------------------
    DECLARE @pk sysname;
    SELECT @pk = kc.name
    FROM sys.key_constraints kc
    WHERE kc.type = 'PK' AND kc.parent_object_id = OBJECT_ID('dbo.Users');
    IF @pk IS NOT NULL 
    BEGIN
        DECLARE @sql nvarchar(max) = N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(@pk) + N';';
        EXEC sp_executesql @sql;
    END

    DECLARE @uqSql nvarchar(max) = N'';
    SELECT @uqSql = @uqSql + N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(k.name) + N';' + CHAR(10)
    FROM sys.key_constraints k
    JOIN sys.index_columns ic ON k.parent_object_id = ic.object_id AND k.unique_index_id = ic.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE k.type = 'UQ' AND k.parent_object_id = OBJECT_ID('dbo.Users') AND c.name IN ('Username', 'Email');
    IF @uqSql <> N'' EXEC sp_executesql @uqSql;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
        DROP INDEX IX_Users_Username ON dbo.Users;

    --------------------------------------------------------
    -- Drop default constraints so column changes succeed
    --------------------------------------------------------
    DECLARE @defSql nvarchar(max) = N'';
    SELECT @defSql = @defSql + N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
    FROM sys.default_constraints dc
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Users')
      AND COL_NAME(dc.parent_object_id, dc.parent_column_id) IN ('CreatedAt', 'Email', 'FirstName', 'LastName', 'PhoneNumber', 'Bio', 'Location', 'ProfileImageUrl', 'UpdatedAt', 'IsDeleted', 'DeletedAt');
    IF @defSql <> N'' EXEC sp_executesql @defSql;

    --------------------------------------------------------
    -- Rename primary key column and drop unused columns
    --------------------------------------------------------
    EXEC sp_rename 'dbo.Users.UserId', 'Id', 'COLUMN';

    ALTER TABLE dbo.Users DROP COLUMN Email, FirstName, LastName, PhoneNumber, Bio, Location, ProfileImageUrl, UpdatedAt, IsDeleted, DeletedAt;

    --------------------------------------------------------
    -- Apply new column definitions and constraints
    --------------------------------------------------------
    ALTER TABLE dbo.Users ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.Users ALTER COLUMN Username NVARCHAR(15) NOT NULL;
    ALTER TABLE dbo.Users ALTER COLUMN CreatedAt DATETIMEOFFSET NOT NULL;

    ALTER TABLE dbo.Users ADD CONSTRAINT PK_Users PRIMARY KEY NONCLUSTERED (Id);
END
ELSE
BEGIN
    PRINT 'Users reshape already applied; ensuring supporting objects exist.';
END

------------------------------------------------------------
-- Ensure indexes and foreign keys exist for the new shape
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Username ON dbo.Users (Username);
END

IF OBJECT_ID('dbo.UserProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserProfiles
    (
        Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
            CONSTRAINT PK_UserProfiles PRIMARY KEY NONCLUSTERED,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        FirstName NVARCHAR(24)     NOT NULL,
        LastName  NVARCHAR(24)     NOT NULL,
        Location  NVARCHAR(100)    NULL,
        Bio       NVARCHAR(1000)   NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserProfiles_UserId' AND object_id = OBJECT_ID('dbo.UserProfiles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserProfiles_UserId ON dbo.UserProfiles (UserId);
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserProfiles_UserId')
    ALTER TABLE dbo.UserProfiles ADD CONSTRAINT FK_UserProfiles_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Schools_Users')
    ALTER TABLE dbo.Schools ADD CONSTRAINT FK_Schools_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Passwords_Users')
    ALTER TABLE dbo.Passwords ADD CONSTRAINT FK_Passwords_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmailConfirmations_Users')
    ALTER TABLE dbo.EmailConfirmations ADD CONSTRAINT FK_EmailConfirmations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ResetPasswordTokens_Users')
    ALTER TABLE dbo.ResetPasswordTokens ADD CONSTRAINT FK_ResetPasswordTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRole_User')
    ALTER TABLE dbo.UserRole ADD CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Posts_Users')
    ALTER TABLE dbo.Posts ADD CONSTRAINT FK_Posts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Comments_Users')
    ALTER TABLE dbo.Comments ADD CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Likes_Users')
    ALTER TABLE dbo.Likes ADD CONSTRAINT FK_Likes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRelationships_Follower')
    ALTER TABLE dbo.UserRelationships ADD CONSTRAINT FK_UserRelationships_Follower FOREIGN KEY (FollowerUserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRelationships_Following')
    ALTER TABLE dbo.UserRelationships ADD CONSTRAINT FK_UserRelationships_Following FOREIGN KEY (FollowingUserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Users')
    ALTER TABLE dbo.Notifications ADD CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Notifications_Actor')
    ALTER TABLE dbo.Notifications ADD CONSTRAINT FK_Notifications_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.Users (Id);

------------------------------------------------------------
-- Refresh SoftDeleteUser proc to align with new Users shape
------------------------------------------------------------
IF OBJECT_ID('dbo.SoftDeleteUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SoftDeleteUser;
GO

CREATE PROCEDURE dbo.SoftDeleteUser
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @now DATETIME2 = SYSUTCDATETIME();

    -- Users table keeps the row; mark related entities as soft-deleted
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

PRINT 'Users/UserProfiles reshape completed.';
