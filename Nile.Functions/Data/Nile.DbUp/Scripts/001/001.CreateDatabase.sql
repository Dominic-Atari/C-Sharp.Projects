-- Create Nile Database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NileDb')
BEGIN
    CREATE DATABASE NileDb;
    PRINT 'Database NileDb created successfully.';
END
ELSE
BEGIN
    PRINT 'Database NileDb already exists.';
END
GO
