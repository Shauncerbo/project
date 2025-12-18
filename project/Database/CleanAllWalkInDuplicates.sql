-- =====================================================
-- CLEAN ALL WALK-IN DUPLICATES - RUN THIS IN SSMS
-- Run on BOTH local and online databases
-- =====================================================

-- First, check how many duplicates exist
SELECT 'BEFORE CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalWalkIns FROM WalkIns;

-- Show duplicate groups
SELECT 
    FirstName, 
    LastName, 
    CAST(VisitDate AS DATE) AS VisitDate,
    PaymentAmount,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(WalkInID AS VARCHAR), ', ') AS WalkInIDs
FROM WalkIns
WHERE IsArchived = 0
GROUP BY FirstName, LastName, CAST(VisitDate AS DATE), PaymentAmount
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC, VisitDate DESC;

-- =====================================================
-- STEP 1: Delete all duplicate walk-ins (keep lowest WalkInID)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        WalkInID,
        FirstName,
        LastName,
        CAST(VisitDate AS DATE) AS VisitDate,
        PaymentAmount,
        ROW_NUMBER() OVER (
            PARTITION BY FirstName, LastName, CAST(VisitDate AS DATE), PaymentAmount 
            ORDER BY WalkInID
        ) AS RowNum
    FROM WalkIns
    WHERE IsArchived = 0
)
DELETE FROM WalkIns
WHERE WalkInID IN (
    SELECT WalkInID FROM DuplicateGroups WHERE RowNum > 1
);

SELECT 'Deleted duplicate walk-ins' AS Status;

-- =====================================================
-- STEP 2: Verify cleanup
-- =====================================================
SELECT 'AFTER CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalWalkIns FROM WalkIns;

-- Check if any duplicates remain
SELECT 
    FirstName, 
    LastName, 
    CAST(VisitDate AS DATE) AS VisitDate,
    PaymentAmount,
    COUNT(*) AS DuplicateCount
FROM WalkIns
WHERE IsArchived = 0
GROUP BY FirstName, LastName, CAST(VisitDate AS DATE), PaymentAmount
HAVING COUNT(*) > 1;

SELECT 'If no rows above, all duplicates are cleaned!' AS Status;

