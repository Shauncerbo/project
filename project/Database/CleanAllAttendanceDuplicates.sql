-- =====================================================
-- CLEAN ALL ATTENDANCE DUPLICATES - RUN THIS IN SSMS
-- Run on BOTH local and online databases
-- =====================================================

USE GymCRM_DB;
GO

-- First, check how many duplicates exist
SELECT 'BEFORE CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalAttendances FROM Attendances;

-- Show duplicate groups
SELECT 
    MemberID,
    CAST(CheckinTime AS DATE) AS CheckinDate,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(AttendanceID AS VARCHAR), ', ') AS AttendanceIDs
FROM Attendances
GROUP BY MemberID, CAST(CheckinTime AS DATE)
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- =====================================================
-- STEP 1: Delete all duplicate attendances (keep lowest AttendanceID)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        AttendanceID,
        MemberID,
        CAST(CheckinTime AS DATE) AS CheckinDate,
        ROW_NUMBER() OVER (
            PARTITION BY MemberID, CAST(CheckinTime AS DATE) 
            ORDER BY AttendanceID
        ) AS RowNum
    FROM Attendances
)
DELETE FROM Attendances
WHERE AttendanceID IN (
    SELECT AttendanceID FROM DuplicateGroups WHERE RowNum > 1
);

SELECT 'Deleted duplicate attendances' AS Status;

-- =====================================================
-- STEP 2: Verify cleanup
-- =====================================================
SELECT 'AFTER CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalAttendances FROM Attendances;

-- Check if any duplicates remain
SELECT 
    MemberID,
    CAST(CheckinTime AS DATE) AS CheckinDate,
    COUNT(*) AS DuplicateCount
FROM Attendances
GROUP BY MemberID, CAST(CheckinTime AS DATE)
HAVING COUNT(*) > 1;

SELECT 'If no rows above, all duplicates are cleaned!' AS Status;

