-- =====================================================
-- CLEAN ALL MEMBER DUPLICATES - RUN THIS IN SSMS
-- Run on BOTH local and online databases
-- =====================================================

USE GymCRM_DB;
GO

-- First, check how many duplicates exist
SELECT 'BEFORE CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalMembers FROM Members WHERE IsArchived = 0;

-- Show duplicate groups
SELECT 
    FirstName, 
    LastName, 
    ContactNumber,
    Email,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(MemberID AS VARCHAR), ', ') AS MemberIDs
FROM Members
WHERE IsArchived = 0
GROUP BY FirstName, LastName, ContactNumber, Email
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- =====================================================
-- STEP 1: Create temp table with duplicate member mapping
-- =====================================================
-- Create temp table to store duplicate member mappings
IF OBJECT_ID('tempdb..#DuplicateMemberMapping') IS NOT NULL
    DROP TABLE #DuplicateMemberMapping;

SELECT 
    MemberID,
    MIN(MemberID) OVER (
        PARTITION BY FirstName, LastName, ContactNumber, Email
    ) AS KeeperID
INTO #DuplicateMemberMapping
FROM Members
WHERE IsArchived = 0
AND MemberID NOT IN (
    -- Exclude the keeper (lowest ID) from each group
    SELECT MIN(MemberID)
    FROM Members
    WHERE IsArchived = 0
    GROUP BY FirstName, LastName, ContactNumber, Email
);

-- Update Payments
UPDATE p
SET p.MemberID = dmm.KeeperID
FROM Payments p
INNER JOIN #DuplicateMemberMapping dmm ON p.MemberID = dmm.MemberID;

-- Update Attendances
UPDATE a
SET a.MemberID = dmm.KeeperID
FROM Attendances a
INNER JOIN #DuplicateMemberMapping dmm ON a.MemberID = dmm.MemberID;

-- Update Notifications
UPDATE n
SET n.MemberID = dmm.KeeperID
FROM Notifications n
INNER JOIN #DuplicateMemberMapping dmm ON n.MemberID = dmm.MemberID;

-- Update MemberPromos (handle unique constraint conflicts)
-- First, delete MemberPromos that would create duplicates after update
DELETE mp
FROM MemberPromos mp
INNER JOIN #DuplicateMemberMapping dmm ON mp.MemberID = dmm.MemberID
WHERE EXISTS (
    SELECT 1 
    FROM MemberPromos existing
    WHERE existing.MemberID = dmm.KeeperID 
    AND existing.PromotionID = mp.PromotionID
);

-- Then update the remaining MemberPromos
UPDATE mp
SET mp.MemberID = dmm.KeeperID
FROM MemberPromos mp
INNER JOIN #DuplicateMemberMapping dmm ON mp.MemberID = dmm.MemberID;

-- Update MemberTrainers
UPDATE mt
SET mt.MemberID = dmm.KeeperID
FROM MemberTrainers mt
INNER JOIN #DuplicateMemberMapping dmm ON mt.MemberID = dmm.MemberID;

SELECT 'Updated foreign key references to use keeper MemberID' AS Status;

-- Verify no foreign key references remain to duplicate members
SELECT 'Verifying foreign key references...' AS Status;
DECLARE @RemainingRefs INT = 0;

SELECT @RemainingRefs = COUNT(*)
FROM (
    SELECT MemberID FROM Payments WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping)
    UNION
    SELECT MemberID FROM Attendances WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping)
    UNION
    SELECT MemberID FROM Notifications WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping)
    UNION
    SELECT MemberID FROM MemberPromos WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping)
    UNION
    SELECT MemberID FROM MemberTrainers WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping)
) AS Remaining;

IF @RemainingRefs > 0
BEGIN
    PRINT 'WARNING: ' + CAST(@RemainingRefs AS VARCHAR(10)) + ' foreign key references still point to duplicate members.';
    PRINT 'These will be deleted along with the duplicate members.';
END
ELSE
BEGIN
    PRINT 'All foreign key references have been updated successfully.';
END

-- =====================================================
-- STEP 2: Delete any remaining foreign key references to duplicate members
-- =====================================================
-- Delete MemberPromos that still reference duplicate members
DELETE FROM MemberPromos
WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping);

-- Delete MemberTrainers that still reference duplicate members
DELETE FROM MemberTrainers
WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping);

-- Delete Notifications that still reference duplicate members
DELETE FROM Notifications
WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping);

-- Delete Attendances that still reference duplicate members
DELETE FROM Attendances
WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping);

-- Delete Payments that still reference duplicate members
DELETE FROM Payments
WHERE MemberID IN (SELECT MemberID FROM #DuplicateMemberMapping);

SELECT 'Deleted remaining foreign key references to duplicate members' AS Status;

-- Clean up temp table
DROP TABLE #DuplicateMemberMapping;

-- =====================================================
-- STEP 3: Delete all duplicate members (keep lowest MemberID)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        MemberID,
        ROW_NUMBER() OVER (
            PARTITION BY FirstName, LastName, ContactNumber, Email 
            ORDER BY MemberID
        ) AS RowNum
    FROM Members
    WHERE IsArchived = 0
)
DELETE FROM Members
WHERE MemberID IN (
    SELECT MemberID FROM DuplicateGroups WHERE RowNum > 1
);

SELECT 'Deleted duplicate members' AS Status;

-- =====================================================
-- STEP 4: Verify cleanup
-- =====================================================
SELECT 'AFTER CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalMembers FROM Members WHERE IsArchived = 0;

-- Check if any duplicates remain
SELECT 
    FirstName, 
    LastName, 
    ContactNumber,
    Email,
    COUNT(*) AS DuplicateCount
FROM Members
WHERE IsArchived = 0
GROUP BY FirstName, LastName, ContactNumber, Email
HAVING COUNT(*) > 1;

SELECT 'If no rows above, all duplicates are cleaned!' AS Status;

