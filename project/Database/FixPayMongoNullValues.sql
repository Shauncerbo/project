-- ============================================
-- Fix NULL Values in PayMongo Fields
-- Database: GymCRM_DB
-- ============================================

USE GymCRM_DB;
GO

-- ============================================
-- FIX NULL VALUES IN WALKINS TABLE
-- ============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'WalkIns')
BEGIN
    -- Update NULL IsOnlinePayment values to 0 (false)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WalkIns') AND name = 'IsOnlinePayment')
    BEGIN
        UPDATE WalkIns
        SET IsOnlinePayment = 0
        WHERE IsOnlinePayment IS NULL;
        
        PRINT 'Updated NULL IsOnlinePayment values to 0';
        PRINT 'Rows affected: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
    END
    
    -- Ensure IsOnlinePayment column allows NULL (in case it doesn't)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WalkIns') AND name = 'IsOnlinePayment')
    BEGIN
        -- Check if column is nullable
        IF (SELECT is_nullable FROM sys.columns WHERE object_id = OBJECT_ID('WalkIns') AND name = 'IsOnlinePayment') = 0
        BEGIN
            ALTER TABLE WalkIns
            ALTER COLUMN IsOnlinePayment BIT NULL;
            PRINT 'Changed IsOnlinePayment column to allow NULL values';
        END
    END
    
    PRINT 'PayMongo NULL values fix completed successfully';
END
ELSE
BEGIN
    PRINT 'ERROR: WalkIns table does not exist.';
END
GO


