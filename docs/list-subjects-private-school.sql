-- docs/list-subjects-private-school.sql
-- Lists all subjects that belong to the school named 'Private School'
-- Optional: to parameterize the school name with sqlcmd, uncomment the following line and use $(SchoolName) in the WHERE clause.
-- :setvar SchoolName "Private School"

SET NOCOUNT ON;

IF OBJECT_ID('dbo.Subjects') IS NULL
BEGIN
    PRINT 'Object dbo.Subjects does not exist in this database.';
END
ELSE
BEGIN
    DECLARE @schoolName NVARCHAR(200) = 'Private School'; -- change or parameterize if needed
    DECLARE @sql NVARCHAR(MAX) = N'';

    -- Build SELECT list for subject name (handle different schema variants)
    SET @sql = N'SELECT s.SubjectId';

    IF COL_LENGTH('dbo.Subjects', 'Name') IS NOT NULL
        SET @sql += N', s.Name AS SubjectName';
    ELSE IF COL_LENGTH('dbo.Subjects', 'SubjectName') IS NOT NULL
        SET @sql += N', s.SubjectName AS SubjectName';
    ELSE IF COL_LENGTH('dbo.Subjects', 'Title') IS NOT NULL
        SET @sql += N', s.Title AS SubjectName';
    ELSE
        SET @sql += N', NULL AS SubjectName';

    -- Description-like column
    IF COL_LENGTH('dbo.Subjects', 'Description') IS NOT NULL
        SET @sql += N', s.Description';
    ELSE IF COL_LENGTH('dbo.Subjects', 'Summary') IS NOT NULL
        SET @sql += N', s.Summary AS Description';
    ELSE
        SET @sql += N', NULL AS Description';

    -- SchoolId and school name (school name may be 'Name' or 'SchoolName')
    SET @sql += N', s.SchoolId';
    IF COL_LENGTH('dbo.Schools', 'Name') IS NOT NULL
        SET @sql += N', sch.Name AS SchoolName';
    ELSE IF COL_LENGTH('dbo.Schools', 'SchoolName') IS NOT NULL
        SET @sql += N', sch.SchoolName AS SchoolName';
    ELSE
        SET @sql += N', NULL AS SchoolName';

    -- IsDeleted (default to 0 if missing)
    IF COL_LENGTH('dbo.Subjects', 'IsDeleted') IS NOT NULL
        SET @sql += N', ISNULL(s.IsDeleted, 0) AS IsDeleted';
    ELSE
        SET @sql += N', 0 AS IsDeleted';

    -- CreatedAt (optional)
    IF COL_LENGTH('dbo.Subjects', 'CreatedAt') IS NOT NULL
        SET @sql += N', s.CreatedAt';
    ELSE
        SET @sql += N', NULL AS CreatedAt';

    -- FROM / JOIN
    SET @sql += N' FROM dbo.Subjects s LEFT JOIN dbo.Schools sch ON sch.SchoolId = s.SchoolId';

    -- WHERE: pick the school name column that exists
    IF COL_LENGTH('dbo.Schools', 'Name') IS NOT NULL
        SET @sql += N' WHERE sch.Name = @schoolName';
    ELSE IF COL_LENGTH('dbo.Schools', 'SchoolName') IS NOT NULL
        SET @sql += N' WHERE sch.SchoolName = @schoolName';
    ELSE
        SET @sql += N' WHERE 1=0 -- no recognizable School name column';

    -- Exclude soft-deleted rows when column exists
    IF COL_LENGTH('dbo.Subjects', 'IsDeleted') IS NOT NULL
        SET @sql += N' AND (s.IsDeleted = 0 OR s.IsDeleted IS NULL)';

    -- Order by the subject display name if present
    SET @sql += N' ORDER BY SubjectName';

    EXEC sp_executesql @sql, N'@schoolName NVARCHAR(200)', @schoolName = @schoolName;
END
