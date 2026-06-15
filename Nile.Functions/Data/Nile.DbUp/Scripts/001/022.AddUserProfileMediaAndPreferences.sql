-- Add ProfileImageFilename and NotificationPreferencesJson columns to dbo.UserProfiles
PRINT 'Applying 022.AddUserProfileMediaAndPreferences.sql';

IF OBJECT_ID('dbo.UserProfiles','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UserProfiles','ProfileImageFilename') IS NULL
    BEGIN
        ALTER TABLE dbo.UserProfiles ADD ProfileImageFilename NVARCHAR(2000) NULL;
        PRINT 'Added ProfileImageFilename column to dbo.UserProfiles';
    END
    ELSE
    BEGIN
        PRINT 'dbo.UserProfiles.ProfileImageFilename already exists; skipping';
    END

    IF COL_LENGTH('dbo.UserProfiles','NotificationPreferencesJson') IS NULL
    BEGIN
        ALTER TABLE dbo.UserProfiles ADD NotificationPreferencesJson NVARCHAR(2000) NULL;
        PRINT 'Added NotificationPreferencesJson column to dbo.UserProfiles';
    END
    ELSE
    BEGIN
        PRINT 'dbo.UserProfiles.NotificationPreferencesJson already exists; skipping';
    END
END
ELSE
BEGIN
    PRINT 'dbo.UserProfiles does not exist; skipping migration 022.AddUserProfileMediaAndPreferences.sql';
END
