-- 021.MakeLikesPostIdNullable.sql
-- Make Likes.PostId nullable and recreate FK idempotently for both table-name variants
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Handle table 'Likes' (plural)
IF OBJECT_ID('dbo.Likes','U') IS NOT NULL
BEGIN
    DECLARE @fk nvarchar(200);
    SELECT TOP 1 @fk = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = fkc.parent_column_id
    WHERE t.name = 'Likes' AND c.name = 'PostId';

    IF @fk IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE dbo.Likes DROP CONSTRAINT [' + @fk + ']');
    END

    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Likes') AND name = 'PostId' AND is_nullable = 0)
    BEGIN
        ALTER TABLE dbo.Likes ALTER COLUMN PostId UNIQUEIDENTIFIER NULL;
    END

    DECLARE @postRef nvarchar(200) = NULL;
    IF OBJECT_ID('dbo.Post') IS NOT NULL SET @postRef = 'dbo.Post';
    ELSE IF OBJECT_ID('dbo.Posts') IS NOT NULL SET @postRef = 'dbo.Posts';

    IF @postRef IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.Likes') AND referenced_object_id = OBJECT_ID(@postRef))
        BEGIN
            EXEC('ALTER TABLE dbo.Likes WITH CHECK ADD CONSTRAINT FK_Likes_Post_PostId FOREIGN KEY (PostId) REFERENCES ' + @postRef + ' (PostId) ON DELETE CASCADE;');
        END
    END
END

-- Handle table 'Like' (singular)
IF OBJECT_ID('dbo.Like','U') IS NOT NULL
BEGIN
    DECLARE @fk2 nvarchar(200);
    SELECT TOP 1 @fk2 = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = fkc.parent_column_id
    WHERE t.name = 'Like' AND c.name = 'PostId';

    IF @fk2 IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE dbo.[Like] DROP CONSTRAINT [' + @fk2 + ']');
    END

    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.[Like]') AND name = 'PostId' AND is_nullable = 0)
    BEGIN
        ALTER TABLE dbo.[Like] ALTER COLUMN PostId UNIQUEIDENTIFIER NULL;
    END

    DECLARE @postRef2 nvarchar(200) = NULL;
    IF OBJECT_ID('dbo.Post') IS NOT NULL SET @postRef2 = 'dbo.Post';
    ELSE IF OBJECT_ID('dbo.Posts') IS NOT NULL SET @postRef2 = 'dbo.Posts';

    IF @postRef2 IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.[Like]') AND referenced_object_id = OBJECT_ID(@postRef2))
        BEGIN
            EXEC('ALTER TABLE dbo.[Like] WITH CHECK ADD CONSTRAINT FK_Like_Post_PostId FOREIGN KEY (PostId) REFERENCES ' + @postRef2 + ' (PostId) ON DELETE CASCADE;');
        END
    END
END

COMMIT TRANSACTION;
