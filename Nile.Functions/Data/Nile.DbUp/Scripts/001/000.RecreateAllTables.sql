-- 000.RecreateAllTables.sql
-- Drops and recreates core tables used by the app and inserts minimal seed data for testing.
-- WARNING: This script will DROP TABLES. Use only in local/test environments.
SET NOCOUNT ON;

-- Drop child tables first (if they exist) to avoid FK problems
IF OBJECT_ID('dbo.Stories','U') IS NOT NULL DROP TABLE dbo.Stories;
IF OBJECT_ID('dbo.SubTopics','U') IS NOT NULL DROP TABLE dbo.SubTopics;
IF OBJECT_ID('dbo.Stages','U') IS NOT NULL DROP TABLE dbo.Stages;
IF OBJECT_ID('dbo.Topics','U') IS NOT NULL DROP TABLE dbo.Topics;
IF OBJECT_ID('dbo.TeacherSubjects','U') IS NOT NULL DROP TABLE dbo.TeacherSubjects;
IF OBJECT_ID('dbo.Subjects','U') IS NOT NULL DROP TABLE dbo.Subjects;
IF OBJECT_ID('dbo.Schools','U') IS NOT NULL DROP TABLE dbo.Schools;
IF OBJECT_ID('dbo.SchemaVersions','U') IS NOT NULL DROP TABLE dbo.SchemaVersions;

-- Create Schools
CREATE TABLE dbo.Schools (
    SchoolId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NULL,
    HeadTeacherUserId UNIQUEIDENTIFIER NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL
);

-- Create Subjects
CREATE TABLE dbo.Subjects (
    SubjectId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL
);

-- Create TeacherSubjects mapping
CREATE TABLE dbo.TeacherSubjects (
    TeacherSubjectId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Create Topics (legacy store still keeps Subtopic text for compatibility)
CREATE TABLE dbo.Topics (
    TopicId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NULL,
    Subtopic NVARCHAR(200) NULL, -- legacy
    Notes NVARCHAR(MAX) NULL,
    ParentTopicId UNIQUEIDENTIFIER NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL
);

-- Create SubTopics (new first-class entity)
CREATE TABLE dbo.SubTopics (
    SubTopicId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    TopicId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL
);

CREATE UNIQUE INDEX IX_SubTopics_TopicId_Name ON dbo.SubTopics(TopicId, Name);

-- Create Stages
CREATE TABLE dbo.Stages (
    StageId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NULL,
    TopicId UNIQUEIDENTIFIER NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

-- Create Stories
CREATE TABLE dbo.Stories (
    StoryId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    TopicId UNIQUEIDENTIFIER NULL,
    SubTopicId UNIQUEIDENTIFIER NULL,
    PromptId UNIQUEIDENTIFIER NULL,
    [User] NVARCHAR(200) NULL,
    Payload NVARCHAR(MAX) NULL,
    ClientCorrelationId NVARCHAR(200) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

-- Minimal DbUp journal table (SchemaVersions)
CREATE TABLE dbo.SchemaVersions (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ScriptName NVARCHAR(255) NOT NULL,
    AppliedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Success BIT NOT NULL
);

-- Foreign keys
ALTER TABLE dbo.Topics
    ADD CONSTRAINT FK_Topics_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId);
ALTER TABLE dbo.Topics
    ADD CONSTRAINT FK_Topics_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(SubjectId);

ALTER TABLE dbo.TeacherSubjects
    ADD CONSTRAINT FK_TeacherSubjects_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId);
ALTER TABLE dbo.TeacherSubjects
    ADD CONSTRAINT FK_TeacherSubjects_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(SubjectId);

ALTER TABLE dbo.SubTopics
    ADD CONSTRAINT FK_SubTopics_Topics FOREIGN KEY (TopicId) REFERENCES dbo.Topics(TopicId) ON DELETE NO ACTION;

ALTER TABLE dbo.Stories
    ADD CONSTRAINT FK_Stories_Topics FOREIGN KEY (TopicId) REFERENCES dbo.Topics(TopicId) ON DELETE SET NULL;
ALTER TABLE dbo.Stories
    ADD CONSTRAINT FK_Stories_SubTopics FOREIGN KEY (SubTopicId) REFERENCES dbo.SubTopics(SubTopicId) ON DELETE SET NULL;

ALTER TABLE dbo.Stages
    ADD CONSTRAINT FK_Stages_Schools FOREIGN KEY (SchoolId) REFERENCES dbo.Schools(SchoolId);
ALTER TABLE dbo.Stages
    ADD CONSTRAINT FK_Stages_Topics FOREIGN KEY (TopicId) REFERENCES dbo.Topics(TopicId) ON DELETE SET NULL;

-- Seed minimal test data
PRINT 'Inserting seed data...';

DECLARE @schoolId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @subjectId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @teacherUserId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

INSERT INTO dbo.Schools (SchoolId, Name, HeadTeacherUserId) VALUES (@schoolId, 'Dev School', @teacherUserId);
INSERT INTO dbo.Subjects (SubjectId, Name) VALUES (@subjectId, 'Mathematics');
INSERT INTO dbo.TeacherSubjects (TeacherSubjectId, UserId, SchoolId, SubjectId) VALUES (NEWID(), @teacherUserId, @schoolId, @subjectId);

-- Add a sample topic + subtopic
DECLARE @topicId UNIQUEIDENTIFIER = NEWID();
INSERT INTO dbo.Topics (TopicId, SchoolId, SubjectId, Name, Subtopic, Notes) VALUES (@topicId, @schoolId, @subjectId, 'Numbers', NULL, 'Sample topic');
DECLARE @subTopicId UNIQUEIDENTIFIER = NEWID();
INSERT INTO dbo.SubTopics (SubTopicId, TopicId, Name, Notes) VALUES (@subTopicId, @topicId, 'Counting', 'Sample subtopic');

-- Optionally insert a placeholder story referencing them
INSERT INTO dbo.Stories (StoryId, SchoolId, SubjectId, TopicId, SubTopicId, [User], Payload, CreatedAt) VALUES (NEWID(), @schoolId, @subjectId, @topicId, @subTopicId, 'devuser', '{"text":"seed story"}', SYSUTCDATETIME());

PRINT 'Done.';
GO
 
/* Additional core tables from other DbUp scripts (Users, Accounts, Posts, LMS, etc.)
   These definitions are simplified/local-test friendly versions to allow a single
   recreate-and-seed operation for local development. */

-- Users and profiles
IF OBJECT_ID('dbo.Users','U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.UserProfiles','U') IS NOT NULL DROP TABLE dbo.UserProfiles;
IF OBJECT_ID('dbo.Accounts','U') IS NOT NULL DROP TABLE dbo.Accounts;
IF OBJECT_ID('dbo.Passwords','U') IS NOT NULL DROP TABLE dbo.Passwords;
IF OBJECT_ID('dbo.EmailConfirmations','U') IS NOT NULL DROP TABLE dbo.EmailConfirmations;
IF OBJECT_ID('dbo.ResetPasswordTokens','U') IS NOT NULL DROP TABLE dbo.ResetPasswordTokens;
IF OBJECT_ID('dbo.Role','U') IS NOT NULL DROP TABLE dbo.Role;
IF OBJECT_ID('dbo.UserRole','U') IS NOT NULL DROP TABLE dbo.UserRole;

CREATE TABLE dbo.Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.UserProfiles (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    FirstName NVARCHAR(100) NULL,
    LastName NVARCHAR(100) NULL,
    Location NVARCHAR(200) NULL,
    Bio NVARCHAR(1000) NULL
);

CREATE TABLE dbo.Accounts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    AccountName NVARCHAR(200) NULL,
    EmailAddress NVARCHAR(200) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UserId UNIQUEIDENTIFIER NULL
);

CREATE TABLE dbo.Passwords (
    UserId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PasswordHash NVARCHAR(MAX) NOT NULL
);

CREATE TABLE dbo.EmailConfirmations (
    ConfirmationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ConfirmationToken NVARCHAR(MAX) NOT NULL,
    IsConfirmed BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2 NULL
);

CREATE TABLE dbo.ResetPasswordTokens (
    TokenId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Token NVARCHAR(MAX) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Role (
    RoleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    RoleName NVARCHAR(50) NOT NULL
);

CREATE TABLE dbo.UserRole (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_UserRole PRIMARY KEY (UserId, RoleId)
);

-- Social tables (posts/comments/likes/relationships/notifications)
IF OBJECT_ID('dbo.Posts','U') IS NOT NULL DROP TABLE dbo.Posts;
IF OBJECT_ID('dbo.Comments','U') IS NOT NULL DROP TABLE dbo.Comments;
IF OBJECT_ID('dbo.Likes','U') IS NOT NULL DROP TABLE dbo.Likes;
IF OBJECT_ID('dbo.UserRelationships','U') IS NOT NULL DROP TABLE dbo.UserRelationships;
IF OBJECT_ID('dbo.Notifications','U') IS NOT NULL DROP TABLE dbo.Notifications;

CREATE TABLE dbo.Posts (
    PostId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NULL,
    Content NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(500) NULL,
    PostType NVARCHAR(200) NOT NULL DEFAULT 'Text',
    LikesCount INT NOT NULL DEFAULT 0,
    CommentsCount INT NOT NULL DEFAULT 0,
    SharesCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Comments (
    CommentId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    PostId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(1000) NOT NULL,
    ParentCommentId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Likes (
    LikeId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    PostId UNIQUEIDENTIFIER NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Foreign key for Likes -> Posts (make PostId nullable but keep FK to ensure referential integrity when present)
IF OBJECT_ID('dbo.Posts','U') IS NOT NULL AND OBJECT_ID('dbo.Likes','U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID('dbo.Likes') AND fk.referenced_object_id = OBJECT_ID('dbo.Posts'))
    BEGIN
        ALTER TABLE dbo.Likes WITH CHECK ADD CONSTRAINT FK_Likes_Post_PostId FOREIGN KEY (PostId) REFERENCES dbo.Posts (PostId) ON DELETE CASCADE;
    END
END

CREATE TABLE dbo.UserRelationships (
    RelationshipId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    FollowerUserId UNIQUEIDENTIFIER NOT NULL,
    FollowingUserId UNIQUEIDENTIFIER NOT NULL,
    RelationshipType INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Notifications (
    NotificationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ActorUserId UNIQUEIDENTIFIER NULL,
    NotificationType NVARCHAR(50) NOT NULL,
    EntityType NVARCHAR(50) NULL,
    EntityId UNIQUEIDENTIFIER NULL,
    Message NVARCHAR(500) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- LMS tables: SchoolMemberships, Courses, Lessons, SubLevels
IF OBJECT_ID('dbo.SchoolMemberships','U') IS NOT NULL DROP TABLE dbo.SchoolMemberships;
IF OBJECT_ID('dbo.Courses','U') IS NOT NULL DROP TABLE dbo.Courses;
IF OBJECT_ID('dbo.Lessons','U') IS NOT NULL DROP TABLE dbo.Lessons;
IF OBJECT_ID('dbo.SubLevels','U') IS NOT NULL DROP TABLE dbo.SubLevels;

CREATE TABLE dbo.SchoolMemberships (
    SchoolMembershipId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleInSchool INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Courses (
    CourseId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SchoolId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NULL,
    Title NVARCHAR(200) NOT NULL,
    Summary NVARCHAR(1000) NULL,
    Level NVARCHAR(50) NULL,
    IsPublished BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE dbo.Lessons (
    LessonId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    CourseId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    BodyMarkdown NVARCHAR(MAX) NULL,
    ResourceUrl NVARCHAR(500) NULL,
    [Order] INT NOT NULL DEFAULT 0,
    DurationMinutes INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE dbo.SubLevels (
    SubLevelId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    StageId UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Label] NVARCHAR(200) NULL,
    [Description] NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL
);

PRINT 'Additional core tables created and seeded (local test set).';
