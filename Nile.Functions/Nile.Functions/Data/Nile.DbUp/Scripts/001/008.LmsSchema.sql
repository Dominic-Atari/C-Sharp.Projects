-- LMS schema additions: school ownership fix, memberships, subjects, teacher assignments, courses, lessons
-- Idempotent: safe to rerun

PRINT 'Starting LMS schema migration...';

------------------------------------------------------------
-- Normalize Schools to GUID PK and HeadTeacherUserId
------------------------------------------------------------
IF OBJECT_ID('dbo.Schools', 'U') IS NOT NULL
BEGIN
    DECLARE @schoolIdType sysname = (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Schools' AND COLUMN_NAME = 'SchoolId');

    IF (@schoolIdType = 'int')
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Schools_Users')
            ALTER TABLE dbo.Schools DROP CONSTRAINT FK_Schools_Users;

        CREATE TABLE dbo.Schools_New (
            SchoolId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Schools PRIMARY KEY,
            SchoolName NVARCHAR(100) NOT NULL,
            SchoolAddress NVARCHAR(200) NOT NULL,
            City NVARCHAR(100) NOT NULL,
            State NVARCHAR(100) NOT NULL,
            Country NVARCHAR(100) NOT NULL,
            County NVARCHAR(100) NULL,
            ZipCode NVARCHAR(50) NULL,
            PhoneNumber NVARCHAR(50) NULL,
            Email NVARCHAR(255) NULL,
            Description NVARCHAR(500) NULL,
            ImageUrl NVARCHAR(500) NULL,
            HeadTeacherUserId UNIQUEIDENTIFIER NOT NULL,
            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            UpdatedAt DATETIME2 NULL,
            IsDeleted BIT NOT NULL DEFAULT 0,
            DeletedAt DATETIME2 NULL
        );

        INSERT INTO dbo.Schools_New (
            SchoolId, SchoolName, SchoolAddress, City, State, Country, County, ZipCode, PhoneNumber, Email,
            Description, ImageUrl, HeadTeacherUserId, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
        SELECT NEWID(), SchoolName, SchoolAddress, City, State, Country, County, ZipCode, PhoneNumber, Email,
               Description, ImageUrl, UserId, ISNULL(CreatedAt, SYSUTCDATETIME()), UpdatedAt, IsDeleted, DeletedAt
        FROM dbo.Schools;

        DROP TABLE dbo.Schools;
        EXEC sp_rename 'dbo.Schools_New', 'Schools';
    END

    -- Ensure column exists and is non-nullable even after previous failures
    IF COL_LENGTH('dbo.Schools', 'HeadTeacherUserId') IS NULL
        ALTER TABLE dbo.Schools ADD HeadTeacherUserId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH('dbo.Schools', 'HeadTeacherUserId') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.Schools', 'UserId') IS NOT NULL
        BEGIN
            EXEC('UPDATE s SET HeadTeacherUserId = COALESCE(HeadTeacherUserId, UserId) FROM dbo.Schools s;');
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Schools_Users')
                ALTER TABLE dbo.Schools DROP CONSTRAINT FK_Schools_Users;
            ALTER TABLE dbo.Schools DROP COLUMN UserId;
        END

        EXEC('ALTER TABLE dbo.Schools ALTER COLUMN HeadTeacherUserId UNIQUEIDENTIFIER NOT NULL;');

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Schools_HeadTeacher')
            ALTER TABLE dbo.Schools ADD CONSTRAINT FK_Schools_HeadTeacher FOREIGN KEY (HeadTeacherUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;
    END
END

------------------------------------------------------------
-- SchoolMemberships
------------------------------------------------------------
IF OBJECT_ID('dbo.SchoolMemberships', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchoolMemberships (
        SchoolMembershipId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_SchoolMemberships PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleInSchool INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SchoolMemberships_School_User')
    CREATE UNIQUE NONCLUSTERED INDEX IX_SchoolMemberships_School_User ON dbo.SchoolMemberships (SchoolId, UserId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SchoolMemberships_Schools')
    ALTER TABLE dbo.SchoolMemberships ADD CONSTRAINT FK_SchoolMemberships_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SchoolMemberships_Users')
    ALTER TABLE dbo.SchoolMemberships ADD CONSTRAINT FK_SchoolMemberships_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;

------------------------------------------------------------
-- Subjects
------------------------------------------------------------
IF OBJECT_ID('dbo.Subjects', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subjects (
        SubjectId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Subjects PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        -- Soft-delete flag
        IsDeleted BIT NOT NULL CONSTRAINT DF_Subjects_IsDeleted DEFAULT (0),
        Name NVARCHAR(200) NOT NULL,
        Stage INT NOT NULL,
        Description NVARCHAR(1000) NULL,
        DeletedAt DATETIME2 NULL
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Subjects_School_Name_Stage')
    CREATE UNIQUE NONCLUSTERED INDEX IX_Subjects_School_Name_Stage ON dbo.Subjects (SchoolId, Name, Stage);

------------------------------------------------------------
-- TeacherSubject assignments
------------------------------------------------------------
IF OBJECT_ID('dbo.TeacherSubjects', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeacherSubjects (
        TeacherSubjectId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_TeacherSubjects PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SubjectId UNIQUEIDENTIFIER NOT NULL,
        AssignedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TeacherSubjects_U_S_S')
    CREATE UNIQUE NONCLUSTERED INDEX IX_TeacherSubjects_U_S_S ON dbo.TeacherSubjects (UserId, SchoolId, SubjectId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherSubjects_Users')
    ALTER TABLE dbo.TeacherSubjects ADD CONSTRAINT FK_TeacherSubjects_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherSubjects_Schools')
    ALTER TABLE dbo.TeacherSubjects ADD CONSTRAINT FK_TeacherSubjects_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TeacherSubjects_Subjects')
    ALTER TABLE dbo.TeacherSubjects ADD CONSTRAINT FK_TeacherSubjects_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(SubjectId) ON DELETE CASCADE;

------------------------------------------------------------
-- Courses
------------------------------------------------------------
IF OBJECT_ID('dbo.Courses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Courses (
        CourseId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Courses PRIMARY KEY,
        SchoolId UNIQUEIDENTIFIER NOT NULL,
        SubjectId UNIQUEIDENTIFIER NULL,
        Title NVARCHAR(200) NOT NULL,
        Summary NVARCHAR(1000) NULL,
        Level NVARCHAR(50) NULL,
        IsPublished BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Courses_Schools')
    ALTER TABLE dbo.Courses ADD CONSTRAINT FK_Courses_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Courses_Subjects')
    ALTER TABLE dbo.Courses ADD CONSTRAINT FK_Courses_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(SubjectId) ON DELETE SET NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Courses_School')
    CREATE NONCLUSTERED INDEX IX_Courses_School ON dbo.Courses (SchoolId);

------------------------------------------------------------
-- Lessons
------------------------------------------------------------
IF OBJECT_ID('dbo.Lessons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Lessons (
        LessonId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Lessons PRIMARY KEY,
        CourseId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        BodyMarkdown NVARCHAR(MAX) NULL,
        ResourceUrl NVARCHAR(500) NULL,
        [Order] INT NOT NULL DEFAULT 0,
        DurationMinutes INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Lessons_Courses')
    ALTER TABLE dbo.Lessons ADD CONSTRAINT FK_Lessons_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Lessons_Course_Order')
    CREATE NONCLUSTERED INDEX IX_Lessons_Course_Order ON dbo.Lessons (CourseId, [Order]);

------------------------------------------------------------
-- Seed LMS roles
------------------------------------------------------------
DECLARE @HeadTeacher UNIQUEIDENTIFIER = '4f6e2ad7-8bc0-4b3f-9d4c-4d6c1c6b0f01';
DECLARE @Student UNIQUEIDENTIFIER = 'b8d9f9e3-7a3e-4c1b-9d7a-2f8d2f0c9e13';
DECLARE @Teacher UNIQUEIDENTIFIER = '2c0a9db6-6f45-4d29-9e11-3e6f0b1e5f22';

IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = 'HeadTeacher')
    INSERT INTO dbo.Role (RoleId, RoleName) VALUES (@HeadTeacher, 'HeadTeacher');
IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = 'Student')
    INSERT INTO dbo.Role (RoleId, RoleName) VALUES (@Student, 'Student');
IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE RoleName = 'Teacher')
    INSERT INTO dbo.Role (RoleId, RoleName) VALUES (@Teacher, 'Teacher');

PRINT 'LMS schema migration completed.';
