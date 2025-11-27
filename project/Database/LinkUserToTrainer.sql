-- =============================================
-- Link User Account to Trainer Profile
-- =============================================
-- IMPORTANT: Make sure you're connected to the correct database!
-- For Local SQL Server: USE GymCRM_DB;
-- For MonsterASP.net: USE db32884;
-- =============================================

-- Step 1: Add TrainerID column to Users table (if it doesn't exist)
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'TrainerID'
)
BEGIN
    ALTER TABLE Users
    ADD TrainerID INT NULL;

    ALTER TABLE Users
    ADD CONSTRAINT FK_Users_Trainers
    FOREIGN KEY (TrainerID) REFERENCES Trainers(TrainerID);

    PRINT 'TrainerID column added to Users table.';
END
ELSE
BEGIN
    PRINT 'TrainerID column already exists in Users table.';
END
GO

-- Step 2: Link trainer1 user to Nathaniel Bautista trainer
-- First, find the TrainerID for Nathaniel Bautista
DECLARE @TrainerID INT;
DECLARE @UserID INT;

SELECT @TrainerID = TrainerID 
FROM Trainers 
WHERE FirstName = 'Nathaniel' AND LastName = 'bautista';

SELECT @UserID = UserID 
FROM Users 
WHERE Username = 'trainer1';

IF @TrainerID IS NULL
BEGIN
    PRINT 'ERROR: Nathaniel Bautista not found in Trainers table.';
    PRINT 'Please create the trainer first in the Trainers page.';
END
ELSE IF @UserID IS NULL
BEGIN
    PRINT 'ERROR: User "trainer1" not found in Users table.';
    PRINT 'Please create the user account first in Settings > Account Management.';
END
ELSE
BEGIN
    -- Link the user to the trainer
    UPDATE Users
    SET TrainerID = @TrainerID
    WHERE UserID = @UserID;

    PRINT 'Successfully linked user "trainer1" to trainer "Nathaniel Bautista" (TrainerID: ' + CAST(@TrainerID AS VARCHAR) + ')';
END
GO

-- Step 3: Verify the link
SELECT 
    u.UserID,
    u.Username,
    u.RoleID,
    r.RoleName,
    u.TrainerID,
    t.FirstName + ' ' + t.LastName AS TrainerName,
    t.Specialty
FROM Users u
INNER JOIN Roles r ON u.RoleID = r.RoleID
LEFT JOIN Trainers t ON u.TrainerID = t.TrainerID
WHERE u.Username = 'trainer1';
GO

PRINT '';
PRINT '========================================';
PRINT 'User-Trainer linking completed!';
PRINT '========================================';
PRINT '';
PRINT 'Note: If you need to link other trainer accounts, run:';
PRINT 'UPDATE Users SET TrainerID = <TrainerID> WHERE Username = ''<username>'';';
PRINT '';

