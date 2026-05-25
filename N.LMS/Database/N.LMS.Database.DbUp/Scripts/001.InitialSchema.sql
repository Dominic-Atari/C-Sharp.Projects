-- N.LMS initial schema (MySQL 8+).

CREATE TABLE IF NOT EXISTS `Cohorts` (
    `CohortId` CHAR(36) NOT NULL,
    `Name` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(2000) NULL,
    `StartDate` DATETIME NULL,
    `EndDate` DATETIME NULL,
    `Deleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedUtc` DATETIME NOT NULL,
    `ModifiedUtc` DATETIME NOT NULL,
    PRIMARY KEY (`CohortId`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Users` (
    `UserId` CHAR(36) NOT NULL,
    `FirstName` VARCHAR(100) NOT NULL,
    `LastName` VARCHAR(100) NOT NULL,
    `Email` VARCHAR(320) NOT NULL,
    `PasswordHash` VARCHAR(512) NOT NULL,
    `Role` INT NOT NULL DEFAULT 0,
    `CohortId` CHAR(36) NULL,
    `Deleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedUtc` DATETIME NOT NULL,
    `ModifiedUtc` DATETIME NOT NULL,
    PRIMARY KEY (`UserId`),
    UNIQUE KEY `IX_Users_Email` (`Email`),
    KEY `IX_Users_CohortId` (`CohortId`),
    CONSTRAINT `FK_Users_Cohorts` FOREIGN KEY (`CohortId`) REFERENCES `Cohorts`(`CohortId`) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Courses` (
    `CourseId` CHAR(36) NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(4000) NULL,
    `InstructorId` CHAR(36) NOT NULL,
    `Status` INT NOT NULL DEFAULT 0,
    `PublishedUtc` DATETIME NULL,
    `Deleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedUtc` DATETIME NOT NULL,
    `ModifiedUtc` DATETIME NOT NULL,
    PRIMARY KEY (`CourseId`),
    KEY `IX_Courses_InstructorId` (`InstructorId`),
    CONSTRAINT `FK_Courses_Users` FOREIGN KEY (`InstructorId`) REFERENCES `Users`(`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Enrollments` (
    `EnrollmentId` CHAR(36) NOT NULL,
    `UserId` CHAR(36) NOT NULL,
    `CourseId` CHAR(36) NOT NULL,
    `Role` INT NOT NULL DEFAULT 0,
    `Status` INT NOT NULL DEFAULT 0,
    `EnrolledUtc` DATETIME NOT NULL,
    `CompletedUtc` DATETIME NULL,
    `ProgressPercent` DECIMAL(5,2) NOT NULL DEFAULT 0,
    `Deleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedUtc` DATETIME NOT NULL,
    `ModifiedUtc` DATETIME NOT NULL,
    PRIMARY KEY (`EnrollmentId`),
    UNIQUE KEY `IX_Enrollments_User_Course` (`UserId`, `CourseId`),
    KEY `IX_Enrollments_CourseId` (`CourseId`),
    CONSTRAINT `FK_Enrollments_Users` FOREIGN KEY (`UserId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE,
    CONSTRAINT `FK_Enrollments_Courses` FOREIGN KEY (`CourseId`) REFERENCES `Courses`(`CourseId`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Modules` (
    `ModuleId` CHAR(36) NOT NULL,
    `CourseId` CHAR(36) NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(4000) NULL,
    `SortOrder` INT NOT NULL DEFAULT 0,
    `Deleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedUtc` DATETIME NOT NULL,
    `ModifiedUtc` DATETIME NOT NULL,
    PRIMARY KEY (`ModuleId`),
    KEY `IX_Modules_Course_Sort` (`CourseId`, `SortOrder`),
    CONSTRAINT `FK_Modules_Courses` FOREIGN KEY (`CourseId`) REFERENCES `Courses`(`CourseId`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Lessons` (
    `LessonId` CHAR(36) NOT NULL,
    `ModuleId` CHAR(36) NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Type` INT NOT NULL DEFAULT 0,
    `ContentUrl` VARCHAR(1024) NULL,
    `ContentBody` LONGTEXT NULL,
    `DurationMinutes` INT NULL,
    `SortOrder` INT NOT NULL DEFAULT 0,
    `Deleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedUtc` DATETIME NOT NULL,
    `ModifiedUtc` DATETIME NOT NULL,
    PRIMARY KEY (`LessonId`),
    KEY `IX_Lessons_Module_Sort` (`ModuleId`, `SortOrder`),
    CONSTRAINT `FK_Lessons_Modules` FOREIGN KEY (`ModuleId`) REFERENCES `Modules`(`ModuleId`) ON DELETE CASCADE
) ENGINE=InnoDB;
