-- Initial Tables for Nile Social Media Application

CREATE TABLE Users
(
    Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
        CONSTRAINT PK_Users PRIMARY KEY NONCLUSTERED,
    Username  NVARCHAR(15)     NOT NULL,
    CreatedAt DATETIMEOFFSET   NOT NULL
);

CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Username ON Users (Username);

CREATE TABLE UserProfiles
(
    Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()
        CONSTRAINT PK_UserProfiles PRIMARY KEY NONCLUSTERED,
    UserId    UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT FK_UserProfiles_UserId FOREIGN KEY REFERENCES Users (Id),
    FirstName NVARCHAR(24)     NOT NULL,
    LastName  NVARCHAR(24)     NOT NULL,
    Location  NVARCHAR(100)    NULL,
    Bio       NVARCHAR(1000)   NULL
);

CREATE NONCLUSTERED INDEX IX_UserProfiles_UserId ON UserProfiles (UserId);
--Accounts Table
CREATE TABLE Accounts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AccountName NVARCHAR(100) NOT NULL,
    emailAddress NVARCHAR(100) NOT NULL,
    isDeleted BIT NOT NULL DEFAULT 0,
    isActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT FK_Accounts_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
-- School Table
CREATE TABLE Schools (
                         SchoolId INT IDENTITY(1,1) PRIMARY KEY,
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
                         CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                         UpdatedAt DATETIME2 NULL,
                         IsDeleted BIT NOT NULL DEFAULT 0,
                         DeletedAt DATETIME2 NULL,
                         UserId UNIQUEIDENTIFIER NOT NULL,
                         CONSTRAINT FK_Schools_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);


-- Passwords Table
CREATE TABLE Passwords (
    UserId UNIQUEIDENTIFIER NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_Passwords PRIMARY KEY (UserId),
    CONSTRAINT FK_Passwords_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- EmailConfirmations Table
CREATE TABLE EmailConfirmations (
                                    ConfirmationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                                    UserId UNIQUEIDENTIFIER NOT NULL,
                                    ConfirmationToken NVARCHAR(MAX) NOT NULL,
                                    IsConfirmed BIT NOT NULL DEFAULT 0,
                                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                                    ExpiresAt DATETIME2 NULL,
                                    CONSTRAINT FK_EmailConfirmations_Users FOREIGN KEY (UserId) REFERENCES Users(Id)

);

-- ResetPasswordTokens Table
CREATE TABLE ResetPasswordTokens (
                                     TokenId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                                     UserId UNIQUEIDENTIFIER NOT NULL,
                                     Token NVARCHAR(MAX) NOT NULL,
                                     ExpiresAt DATETIME2 NOT NULL,
                                     CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                                     CONSTRAINT FK_ResetPasswordTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id)

);

-- Role
CREATE TABLE Role(
    RoleId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RoleName NVARCHAR(20) NOT NULL
);

-- UserRole Table (Many-to-Many between Users and Roles)
CREATE TABLE UserRole (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_UserRole PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleId) REFERENCES Role(RoleId)
);

-- Posts Table
CREATE TABLE Posts (
    PostId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NULL,
    Content NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(500) NULL,
    PostType NVARCHAR(2000) NOT NULL DEFAULT 'Text', -- Text, Image, Video, Recipe
    LikesCount INT NOT NULL DEFAULT 0,
    CommentsCount INT NOT NULL DEFAULT 0,
    SharesCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_Posts_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Comments Table
CREATE TABLE Comments (
    CommentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PostId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(1000) NOT NULL,
    ParentCommentId UNIQUEIDENTIFIER NULL, -- For nested comments/replies
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_Comments_Posts FOREIGN KEY (PostId) REFERENCES Posts(PostId),
    CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Comments_ParentComment FOREIGN KEY (ParentCommentId) REFERENCES Comments(CommentId)
);

-- Likes Table
CREATE TABLE Likes (
    LikeId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PostId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Likes_Posts FOREIGN KEY (PostId) REFERENCES Posts(PostId),
    CONSTRAINT FK_Likes_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_Likes_PostUser UNIQUE (PostId, UserId)
);

-- UserRelationships Table (Followers/Following)
CREATE TABLE UserRelationships (
    RelationshipId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FollowerUserId UNIQUEIDENTIFIER NOT NULL,
    FollowingUserId UNIQUEIDENTIFIER NOT NULL,
    RelationshipType INT NOT NULL DEFAULT 0, -- Follow, Block, Friend
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_UserRelationships_Follower FOREIGN KEY (FollowerUserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserRelationships_Following FOREIGN KEY (FollowingUserId) REFERENCES Users(Id),
    CONSTRAINT UQ_UserRelationships UNIQUE (FollowerUserId, FollowingUserId, RelationshipType)
);

-- Notifications Table
CREATE TABLE Notifications (
    NotificationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ActorUserId UNIQUEIDENTIFIER NULL, -- User who triggered the notification
    NotificationType NVARCHAR(50) NOT NULL, -- Like, Comment, Follow, Mention
    EntityType NVARCHAR(50) NULL, -- Post, Comment, User
    EntityId UNIQUEIDENTIFIER NULL,
    Message NVARCHAR(500) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Notifications_Actor FOREIGN KEY (ActorUserId) REFERENCES Users(Id)
);

-- Create Indexes for Performance
CREATE INDEX IX_Posts_UserId ON Posts(UserId);
CREATE INDEX IX_Posts_CreatedAt ON Posts(CreatedAt DESC);
CREATE INDEX IX_Comments_PostId ON Comments(PostId);
CREATE INDEX IX_Comments_UserId ON Comments(UserId);
CREATE INDEX IX_Likes_PostId ON Likes(PostId);
CREATE INDEX IX_Likes_UserId ON Likes(UserId);
CREATE INDEX IX_UserRelationships_Follower ON UserRelationships(FollowerUserId);
CREATE INDEX IX_UserRelationships_Following ON UserRelationships(FollowingUserId);
CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);

PRINT 'Initial tables created successfully.';
GO
