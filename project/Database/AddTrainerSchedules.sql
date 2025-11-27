-- =============================================
-- Add Trainer Schedules for Nathaniel Bautista
-- =============================================
-- IMPORTANT: Make sure you're connected to the correct database!
-- For Local SQL Server: USE GymCRM_DB;
-- For MonsterASP.net: USE db32884;
-- =============================================

-- First, find Nathaniel Bautista's TrainerID
-- Replace the TrainerID below with the actual ID from your database
-- You can find it by running: SELECT TrainerID, FirstName, LastName FROM Trainers WHERE FirstName = 'Nathaniel' AND LastName = 'bautista';

-- Example: If Nathaniel Bautista's TrainerID is 1, use that below
-- Adjust the TrainerID value based on your actual data

DECLARE @TrainerID INT;
SELECT @TrainerID = TrainerID FROM Trainers WHERE FirstName = 'Nathaniel' AND LastName = 'bautista';

IF @TrainerID IS NULL
BEGIN
    PRINT 'ERROR: Nathaniel Bautista not found in Trainers table.';
    PRINT 'Please create the trainer first in the Trainers page, then run this script again.';
    RETURN;
END

PRINT 'Found Trainer: Nathaniel Bautista (TrainerID: ' + CAST(@TrainerID AS VARCHAR) + ')';
PRINT 'Adding schedules...';

-- Add Monday Schedule (DayOfWeek: 1 = Monday)
IF NOT EXISTS (
    SELECT 1 FROM TrainerSchedules 
    WHERE TrainerID = @TrainerID 
    AND DayOfWeek = 1 
    AND StartTime = '09:00:00' 
    AND EndTime = '11:00:00'
)
BEGIN
    INSERT INTO TrainerSchedules (TrainerID, DayOfWeek, StartTime, EndTime, IsAvailable)
    VALUES (@TrainerID, 1, '09:00:00', '11:00:00', 1);
    PRINT 'Added: Monday 09:00 AM - 11:00 AM';
END
ELSE
BEGIN
    PRINT 'Schedule already exists: Monday 09:00 AM - 11:00 AM';
END

-- Add Wednesday Schedule (DayOfWeek: 3 = Wednesday)
IF NOT EXISTS (
    SELECT 1 FROM TrainerSchedules 
    WHERE TrainerID = @TrainerID 
    AND DayOfWeek = 3 
    AND StartTime = '09:00:00' 
    AND EndTime = '11:00:00'
)
BEGIN
    INSERT INTO TrainerSchedules (TrainerID, DayOfWeek, StartTime, EndTime, IsAvailable)
    VALUES (@TrainerID, 3, '09:00:00', '11:00:00', 1);
    PRINT 'Added: Wednesday 09:00 AM - 11:00 AM';
END
ELSE
BEGIN
    PRINT 'Schedule already exists: Wednesday 09:00 AM - 11:00 AM';
END

-- Add Friday Schedule (DayOfWeek: 5 = Friday)
IF NOT EXISTS (
    SELECT 1 FROM TrainerSchedules 
    WHERE TrainerID = @TrainerID 
    AND DayOfWeek = 5 
    AND StartTime = '09:00:00' 
    AND EndTime = '11:00:00'
)
BEGIN
    INSERT INTO TrainerSchedules (TrainerID, DayOfWeek, StartTime, EndTime, IsAvailable)
    VALUES (@TrainerID, 5, '09:00:00', '11:00:00', 1);
    PRINT 'Added: Friday 09:00 AM - 11:00 AM';
END
ELSE
BEGIN
    PRINT 'Schedule already exists: Friday 09:00 AM - 11:00 AM';
END

-- Add Saturday Schedule (DayOfWeek: 6 = Saturday)
IF NOT EXISTS (
    SELECT 1 FROM TrainerSchedules 
    WHERE TrainerID = @TrainerID 
    AND DayOfWeek = 6 
    AND StartTime = '10:00:00' 
    AND EndTime = '12:00:00'
)
BEGIN
    INSERT INTO TrainerSchedules (TrainerID, DayOfWeek, StartTime, EndTime, IsAvailable)
    VALUES (@TrainerID, 6, '10:00:00', '12:00:00', 1);
    PRINT 'Added: Saturday 10:00 AM - 12:00 PM';
END
ELSE
BEGIN
    PRINT 'Schedule already exists: Saturday 10:00 AM - 12:00 PM';
END

PRINT '';
PRINT '========================================';
PRINT 'Trainer schedules added successfully!';
PRINT '========================================';
PRINT '';
PRINT 'DayOfWeek Reference:';
PRINT '  0 = Sunday';
PRINT '  1 = Monday';
PRINT '  2 = Tuesday';
PRINT '  3 = Wednesday';
PRINT '  4 = Thursday';
PRINT '  5 = Friday';
PRINT '  6 = Saturday';
PRINT '';

-- Show all schedules for this trainer
SELECT 
    ts.TrainerScheduleID,
    t.FirstName + ' ' + t.LastName AS TrainerName,
    CASE ts.DayOfWeek
        WHEN 0 THEN 'Sunday'
        WHEN 1 THEN 'Monday'
        WHEN 2 THEN 'Tuesday'
        WHEN 3 THEN 'Wednesday'
        WHEN 4 THEN 'Thursday'
        WHEN 5 THEN 'Friday'
        WHEN 6 THEN 'Saturday'
    END AS DayOfWeek,
    CONVERT(VARCHAR(5), ts.StartTime, 108) AS StartTime,
    CONVERT(VARCHAR(5), ts.EndTime, 108) AS EndTime,
    CASE WHEN ts.IsAvailable = 1 THEN 'Available' ELSE 'Unavailable' END AS Status
FROM TrainerSchedules ts
INNER JOIN Trainers t ON ts.TrainerID = t.TrainerID
WHERE ts.TrainerID = @TrainerID
ORDER BY ts.DayOfWeek, ts.StartTime;

GO

