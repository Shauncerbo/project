-- =============================================
-- Diagnostic Query for TrainerSchedules Table
-- Check table structure and data
-- =============================================
USE GymCRM_DB;
GO

-- 1. Check table structure
PRINT '=== TrainerSchedules Table Structure ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainerSchedules'
ORDER BY ORDINAL_POSITION;
GO

-- 2. Check all schedules with readable format
PRINT '';
PRINT '=== All Trainer Schedules ===';
SELECT 
    ts.TrainerScheduleID,
    t.FirstName + ' ' + t.LastName AS TrainerName,
    ts.TrainerID,
    CASE ts.DayOfWeek
        WHEN 0 THEN 'Sunday'
        WHEN 1 THEN 'Monday'
        WHEN 2 THEN 'Tuesday'
        WHEN 3 THEN 'Wednesday'
        WHEN 4 THEN 'Thursday'
        WHEN 5 THEN 'Friday'
        WHEN 6 THEN 'Saturday'
    END AS DayOfWeek,
    ts.DayOfWeek AS DayOfWeekNumber,
    CONVERT(VARCHAR(8), ts.StartTime, 108) AS StartTime_24H,
    CONVERT(VARCHAR(8), ts.EndTime, 108) AS EndTime_24H,
    -- Format as 12-hour with AM/PM
    CASE 
        WHEN DATEPART(HOUR, ts.StartTime) = 0 THEN '12:' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.StartTime) AS VARCHAR), 2) + ' AM'
        WHEN DATEPART(HOUR, ts.StartTime) < 12 THEN CAST(DATEPART(HOUR, ts.StartTime) AS VARCHAR) + ':' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.StartTime) AS VARCHAR), 2) + ' AM'
        WHEN DATEPART(HOUR, ts.StartTime) = 12 THEN '12:' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.StartTime) AS VARCHAR), 2) + ' PM'
        ELSE CAST(DATEPART(HOUR, ts.StartTime) - 12 AS VARCHAR) + ':' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.StartTime) AS VARCHAR), 2) + ' PM'
    END AS StartTime_12H,
    CASE 
        WHEN DATEPART(HOUR, ts.EndTime) = 0 THEN '12:' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.EndTime) AS VARCHAR), 2) + ' AM'
        WHEN DATEPART(HOUR, ts.EndTime) < 12 THEN CAST(DATEPART(HOUR, ts.EndTime) AS VARCHAR) + ':' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.EndTime) AS VARCHAR), 2) + ' AM'
        WHEN DATEPART(HOUR, ts.EndTime) = 12 THEN '12:' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.EndTime) AS VARCHAR), 2) + ' PM'
        ELSE CAST(DATEPART(HOUR, ts.EndTime) - 12 AS VARCHAR) + ':' + RIGHT('0' + CAST(DATEPART(MINUTE, ts.EndTime) AS VARCHAR), 2) + ' PM'
    END AS EndTime_12H,
    CASE WHEN ts.IsAvailable = 1 THEN 'Available' ELSE 'Unavailable' END AS Status,
    -- Check if end time is after start time
    CASE 
        WHEN ts.EndTime > ts.StartTime THEN 'Valid'
        ELSE 'INVALID - End time is before/equal to start time'
    END AS TimeValidation
FROM TrainerSchedules ts
INNER JOIN Trainers t ON ts.TrainerID = t.TrainerID
ORDER BY ts.TrainerID, ts.DayOfWeek, ts.StartTime;
GO

-- 3. Check for potential overlapping schedules on the same day
PRINT '';
PRINT '=== Potential Overlapping Schedules (Same Day) ===';
SELECT 
    t1.TrainerScheduleID AS Schedule1_ID,
    t.FirstName + ' ' + t.LastName AS TrainerName,
    CASE t1.DayOfWeek
        WHEN 0 THEN 'Sunday'
        WHEN 1 THEN 'Monday'
        WHEN 2 THEN 'Tuesday'
        WHEN 3 THEN 'Wednesday'
        WHEN 4 THEN 'Thursday'
        WHEN 5 THEN 'Friday'
        WHEN 6 THEN 'Saturday'
    END AS DayOfWeek,
    CONVERT(VARCHAR(8), t1.StartTime, 108) + ' - ' + CONVERT(VARCHAR(8), t1.EndTime, 108) AS Schedule1_Time,
    t2.TrainerScheduleID AS Schedule2_ID,
    CONVERT(VARCHAR(8), t2.StartTime, 108) + ' - ' + CONVERT(VARCHAR(8), t2.EndTime, 108) AS Schedule2_Time,
    CASE 
        WHEN t1.StartTime < t2.EndTime AND t1.EndTime > t2.StartTime THEN 'OVERLAPS'
        ELSE 'No Overlap'
    END AS OverlapStatus
FROM TrainerSchedules t1
INNER JOIN TrainerSchedules t2 ON t1.TrainerID = t2.TrainerID 
    AND t1.DayOfWeek = t2.DayOfWeek
    AND t1.TrainerScheduleID < t2.TrainerScheduleID
    AND t1.IsAvailable = 1 
    AND t2.IsAvailable = 1
INNER JOIN Trainers t ON t1.TrainerID = t.TrainerID
WHERE t1.StartTime < t2.EndTime AND t1.EndTime > t2.StartTime
ORDER BY t1.TrainerID, t1.DayOfWeek;
GO

-- 4. Check for invalid schedules (end time <= start time)
PRINT '';
PRINT '=== Invalid Schedules (End Time <= Start Time) ===';
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
    CONVERT(VARCHAR(8), ts.StartTime, 108) AS StartTime,
    CONVERT(VARCHAR(8), ts.EndTime, 108) AS EndTime,
    'INVALID' AS Status
FROM TrainerSchedules ts
INNER JOIN Trainers t ON ts.TrainerID = t.TrainerID
WHERE ts.EndTime <= ts.StartTime
ORDER BY ts.TrainerID, ts.DayOfWeek;
GO

-- 5. Count schedules per trainer per day
PRINT '';
PRINT '=== Schedule Count per Trainer per Day ===';
SELECT 
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
    COUNT(*) AS ScheduleCount
FROM TrainerSchedules ts
INNER JOIN Trainers t ON ts.TrainerID = t.TrainerID
WHERE ts.IsAvailable = 1
GROUP BY t.TrainerID, t.FirstName, t.LastName, ts.DayOfWeek
HAVING COUNT(*) > 1
ORDER BY t.TrainerID, ts.DayOfWeek;
GO

PRINT '';
PRINT '=== Diagnostic Complete ===';
GO

















