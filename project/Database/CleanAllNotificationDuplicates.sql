-- =====================================================
-- CLEAN ALL NOTIFICATION DUPLICATES - RUN THIS IN SSMS
-- Run on BOTH local and online databases
-- =====================================================

USE GymCRM_DB;
GO

-- First, check how many duplicates exist
SELECT 'BEFORE CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalNotifications FROM Notifications;

-- Show duplicate groups
SELECT 
    MemberID,
    Message,
    CAST(DateSent AS DATE) AS DateSent,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(NotificationID AS VARCHAR), ', ') AS NotificationIDs
FROM Notifications
GROUP BY MemberID, Message, CAST(DateSent AS DATE)
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- =====================================================
-- STEP 1: Delete all duplicate notifications (keep lowest NotificationID)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        NotificationID,
        MemberID,
        Message,
        CAST(DateSent AS DATE) AS DateSent,
        ROW_NUMBER() OVER (
            PARTITION BY MemberID, Message, CAST(DateSent AS DATE) 
            ORDER BY NotificationID
        ) AS RowNum
    FROM Notifications
)
DELETE FROM Notifications
WHERE NotificationID IN (
    SELECT NotificationID FROM DuplicateGroups WHERE RowNum > 1
);

SELECT 'Deleted duplicate notifications' AS Status;

-- =====================================================
-- STEP 2: Verify cleanup
-- =====================================================
SELECT 'AFTER CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalNotifications FROM Notifications;

-- Check if any duplicates remain
SELECT 
    MemberID,
    Message,
    CAST(DateSent AS DATE) AS DateSent,
    COUNT(*) AS DuplicateCount
FROM Notifications
GROUP BY MemberID, Message, CAST(DateSent AS DATE)
HAVING COUNT(*) > 1;

SELECT 'If no rows above, all duplicates are cleaned!' AS Status;

