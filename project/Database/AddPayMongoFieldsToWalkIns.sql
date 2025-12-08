-- ============================================
-- Add PayMongo Payment Fields to WalkIns Table
-- Database: GymCRM_DB
-- ============================================

USE GymCRM_DB;
GO

-- ============================================
-- ADD PAYMONGO FIELDS TO WALKINS TABLE
-- ============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'WalkIns')
BEGIN
    -- Check if columns don't exist before adding
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WalkIns') AND name = 'PayMongoPaymentId')
    BEGIN
        ALTER TABLE WalkIns
        ADD PayMongoPaymentId NVARCHAR(255) NULL;
        PRINT 'PayMongoPaymentId column added to WalkIns table';
    END
    ELSE
    BEGIN
        PRINT 'PayMongoPaymentId column already exists in WalkIns table';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WalkIns') AND name = 'PayMongoStatus')
    BEGIN
        ALTER TABLE WalkIns
        ADD PayMongoStatus NVARCHAR(50) NULL;
        PRINT 'PayMongoStatus column added to WalkIns table';
    END
    ELSE
    BEGIN
        PRINT 'PayMongoStatus column already exists in WalkIns table';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WalkIns') AND name = 'IsOnlinePayment')
    BEGIN
        ALTER TABLE WalkIns
        ADD IsOnlinePayment BIT NULL DEFAULT 0;
        PRINT 'IsOnlinePayment column added to WalkIns table';
    END
    ELSE
    BEGIN
        PRINT 'IsOnlinePayment column already exists in WalkIns table';
        -- Update any NULL values to 0 for existing records
        UPDATE WalkIns
        SET IsOnlinePayment = 0
        WHERE IsOnlinePayment IS NULL;
        PRINT 'Updated NULL IsOnlinePayment values to 0';
    END

    PRINT 'PayMongo fields migration completed successfully';
END
ELSE
BEGIN
    PRINT 'ERROR: WalkIns table does not exist. Please create the table first.';
END
GO

