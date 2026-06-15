-- Null out Topic.Subtopic to avoid duplication while we migrate clients to use SubTopics
-- Only run if the column exists. If it's NOT NULL, alter to allow NULL first.
IF COL_LENGTH('dbo.Topics','Subtopic') IS NOT NULL
BEGIN
	-- If column currently does not allow NULLs, alter to make it nullable (use NVARCHAR(200) as common type)
	IF (SELECT CASE WHEN COLUMNPROPERTY(OBJECT_ID('dbo.Topics'), 'Subtopic', 'AllowsNull') = 1 THEN 1 ELSE 0 END) = 0
	BEGIN
		ALTER TABLE dbo.Topics ALTER COLUMN Subtopic NVARCHAR(200) NULL;
	END

	UPDATE dbo.Topics
	SET Subtopic = NULL
	WHERE Subtopic IS NOT NULL;
END
