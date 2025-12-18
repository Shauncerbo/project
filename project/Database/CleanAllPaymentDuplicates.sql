-- =====================================================
-- CLEAN ALL PAYMENT DUPLICATES - RUN THIS IN SSMS
-- Run on BOTH local and online databases
-- =====================================================

USE GymCRM_DB;
GO

-- First, check how many duplicates exist
SELECT 'BEFORE CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalPayments FROM Payments;

-- Show duplicate groups
SELECT 
    MemberID,
    Amount AS PaymentAmount,
    CAST(PaymentDate AS DATE) AS PaymentDate,
    PaymentType AS PaymentMethod,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(PaymentID AS VARCHAR), ', ') AS PaymentIDs
FROM Payments
GROUP BY MemberID, Amount, CAST(PaymentDate AS DATE), PaymentType
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- =====================================================
-- STEP 1: Delete all duplicate payments (keep lowest PaymentID)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        PaymentID,
        MemberID,
        Amount,
        CAST(PaymentDate AS DATE) AS PaymentDate,
        PaymentType,
        ROW_NUMBER() OVER (
            PARTITION BY MemberID, Amount, CAST(PaymentDate AS DATE), PaymentType 
            ORDER BY PaymentID
        ) AS RowNum
    FROM Payments
)
DELETE FROM Payments
WHERE PaymentID IN (
    SELECT PaymentID FROM DuplicateGroups WHERE RowNum > 1
);

SELECT 'Deleted duplicate payments' AS Status;

-- =====================================================
-- STEP 2: Verify cleanup
-- =====================================================
SELECT 'AFTER CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalPayments FROM Payments;

-- Check if any duplicates remain
SELECT 
    MemberID,
    Amount AS PaymentAmount,
    CAST(PaymentDate AS DATE) AS PaymentDate,
    PaymentType AS PaymentMethod,
    COUNT(*) AS DuplicateCount
FROM Payments
GROUP BY MemberID, Amount, CAST(PaymentDate AS DATE), PaymentType
HAVING COUNT(*) > 1;

SELECT 'If no rows above, all duplicates are cleaned!' AS Status;

