-- =====================================================
-- CLEAN ALL PROMOTION DUPLICATES - RUN THIS IN SSMS
-- Run on BOTH local and online databases
-- =====================================================

-- First, check how many duplicates exist
SELECT 'BEFORE CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalPromotions FROM Promotions;

-- Show duplicate groups
SELECT 
    PromoName, 
    StartDate, 
    EndDate, 
    DiscountRate, 
    IsArchived,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(PromoID AS VARCHAR), ', ') AS PromoIDs
FROM Promotions
GROUP BY PromoName, CAST(StartDate AS DATE), CAST(EndDate AS DATE), DiscountRate, IsArchived
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- =====================================================
-- STEP 1: Update MemberPromos to use the lowest PromoID (keeper)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        PromoID,
        PromoName,
        CAST(StartDate AS DATE) AS StartDate,
        CAST(EndDate AS DATE) AS EndDate,
        DiscountRate,
        IsArchived,
        ROW_NUMBER() OVER (
            PARTITION BY PromoName, CAST(StartDate AS DATE), CAST(EndDate AS DATE), DiscountRate, IsArchived 
            ORDER BY PromoID
        ) AS RowNum,
        MIN(PromoID) OVER (
            PARTITION BY PromoName, CAST(StartDate AS DATE), CAST(EndDate AS DATE), DiscountRate, IsArchived
        ) AS KeeperID
    FROM Promotions
)
UPDATE mp
SET mp.PromoID = dg.KeeperID
FROM MemberPromos mp
INNER JOIN DuplicateGroups dg ON mp.PromoID = dg.PromoID
WHERE dg.RowNum > 1;

SELECT 'Updated MemberPromos to use keeper PromoID' AS Status;

-- =====================================================
-- STEP 2: Delete all duplicate promotions (keep lowest PromoID)
-- =====================================================
;WITH DuplicateGroups AS (
    SELECT 
        PromoID,
        ROW_NUMBER() OVER (
            PARTITION BY PromoName, CAST(StartDate AS DATE), CAST(EndDate AS DATE), DiscountRate, IsArchived 
            ORDER BY PromoID
        ) AS RowNum
    FROM Promotions
)
DELETE FROM Promotions
WHERE PromoID IN (
    SELECT PromoID FROM DuplicateGroups WHERE RowNum > 1
);

SELECT 'Deleted duplicate promotions' AS Status;

-- =====================================================
-- STEP 3: Verify cleanup
-- =====================================================
SELECT 'AFTER CLEANUP:' AS Status;
SELECT COUNT(*) AS TotalPromotions FROM Promotions;

-- Check if any duplicates remain
SELECT 
    PromoName, 
    StartDate, 
    EndDate, 
    DiscountRate, 
    IsArchived,
    COUNT(*) AS DuplicateCount
FROM Promotions
GROUP BY PromoName, CAST(StartDate AS DATE), CAST(EndDate AS DATE), DiscountRate, IsArchived
HAVING COUNT(*) > 1;

SELECT 'If no rows above, all duplicates are cleaned!' AS Status;

