-- Align Accounts table schema with EF entity Nile.Database.Entities.Account
-- This script is idempotent and can be executed multiple times safely.

PRINT 'Starting Accounts table alignment...';

IF OBJECT_ID('dbo.Accounts', 'U') IS NULL
BEGIN
    PRINT 'Accounts table does not exist. Skipping alignment script.';
    RETURN;
END

-- 1) Rename columns to PascalCase / desired names if they exist with old casing
IF COL_LENGTH('dbo.Accounts', 'emailAddress') IS NOT NULL AND COL_LENGTH('dbo.Accounts', 'EmailAddress') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Accounts.emailAddress', 'EmailAddress', 'COLUMN';
END

IF COL_LENGTH('dbo.Accounts', 'isDeleted') IS NOT NULL AND COL_LENGTH('dbo.Accounts', 'IsDeleted') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Accounts.isDeleted', 'IsDeleted', 'COLUMN';
END

IF COL_LENGTH('dbo.Accounts', 'isActive') IS NOT NULL AND COL_LENGTH('dbo.Accounts', 'IsActive') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Accounts.isActive', 'IsActive', 'COLUMN';
END

-- Primary key column rename: Id -> AccountId (only if AccountId does not already exist)
IF COL_LENGTH('dbo.Accounts', 'Id') IS NOT NULL AND COL_LENGTH('dbo.Accounts', 'AccountId') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Accounts.Id', 'AccountId', 'COLUMN';
END

-- 2) Add missing columns
IF COL_LENGTH('dbo.Accounts', 'ExternalAuthId') IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD ExternalAuthId NVARCHAR(200) NULL;
END

IF COL_LENGTH('dbo.Accounts', 'ExternalCustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD ExternalCustomerId UNIQUEIDENTIFIER NULL;
END

IF COL_LENGTH('dbo.Accounts', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD Status INT NOT NULL CONSTRAINT DF_Accounts_Status DEFAULT(0);
END

IF COL_LENGTH('dbo.Accounts', 'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD UpdatedAt DATETIMEOFFSET NULL;
END

-- 3) Ensure CreatedAt is DATETIMEOFFSET with SYSUTCDATETIME() default
-- Drop existing default constraint on CreatedAt (if any), then alter type and set new default
DECLARE @dfName sysname;
SELECT @dfName = df.name
FROM sys.default_constraints df
JOIN sys.columns c ON c.default_object_id = df.object_id
JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = 'Accounts' AND c.name = 'CreatedAt';

IF @dfName IS NOT NULL
BEGIN
    DECLARE @dropConstraintSql NVARCHAR(MAX) = N'ALTER TABLE dbo.Accounts DROP CONSTRAINT ' + QUOTENAME(@dfName) + N';';
    EXEC(@dropConstraintSql);
END

-- If the column exists, alter its type (no-op if already DATETIMEOFFSET)
IF COL_LENGTH('dbo.Accounts', 'CreatedAt') IS NOT NULL
BEGIN
    BEGIN TRY
        ALTER TABLE dbo.Accounts ALTER COLUMN CreatedAt DATETIMEOFFSET NOT NULL;
    END TRY
    BEGIN CATCH
        PRINT 'Warning: could not alter Accounts.CreatedAt to DATETIMEOFFSET. Error: ' + ERROR_MESSAGE();
    END CATCH
END

-- Re-create default constraint if none exists
SET @dfName = NULL;
SELECT @dfName = df.name
FROM sys.default_constraints df
JOIN sys.columns c ON c.default_object_id = df.object_id
JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = 'Accounts' AND c.name = 'CreatedAt';

IF @dfName IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD CONSTRAINT DF_Accounts_CreatedAt DEFAULT SYSUTCDATETIME() FOR CreatedAt;
END

-- 4) Helpful indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_UserId' AND object_id = OBJECT_ID('dbo.Accounts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Accounts_UserId ON dbo.Accounts(UserId);
END

-- Unique index on EmailAddress (only if column exists and index not already present)
IF COL_LENGTH('dbo.Accounts', 'EmailAddress') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_EmailAddress' AND object_id = OBJECT_ID('dbo.Accounts'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Accounts_EmailAddress ON dbo.Accounts(EmailAddress);
END

PRINT 'Accounts table alignment completed.';
