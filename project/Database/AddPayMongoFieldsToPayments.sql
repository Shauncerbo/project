-- Add PayMongo fields to Payments table for membership payments
-- Run this script in SQL Server Management Studio (SSMS)

USE GymCRM_DB;
GO

-- Check if Payments table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    -- Add PayMongoPaymentId column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PayMongoPaymentId')
    BEGIN
        ALTER TABLE Payments
        ADD PayMongoPaymentId NVARCHAR(255) NULL;
        PRINT 'Added PayMongoPaymentId column to Payments table';
    END
    ELSE
    BEGIN
        PRINT 'PayMongoPaymentId column already exists in Payments table';
    END

    -- Add PayMongoStatus column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PayMongoStatus')
    BEGIN
        ALTER TABLE Payments
        ADD PayMongoStatus NVARCHAR(50) NULL;
        PRINT 'Added PayMongoStatus column to Payments table';
    END
    ELSE
    BEGIN
        PRINT 'PayMongoStatus column already exists in Payments table';
    END

    -- Add IsOnlinePayment column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'IsOnlinePayment')
    BEGIN
        ALTER TABLE Payments
        ADD IsOnlinePayment BIT NULL DEFAULT 0;
        PRINT 'Added IsOnlinePayment column to Payments table';
        
        -- Update existing NULL values to 0 (false)
        UPDATE Payments
        SET IsOnlinePayment = 0
        WHERE IsOnlinePayment IS NULL;
        PRINT 'Updated existing NULL IsOnlinePayment values to 0';
    END
    ELSE
    BEGIN
        PRINT 'IsOnlinePayment column already exists in Payments table';
    END
END
ELSE
BEGIN
    PRINT 'ERROR: Payments table does not exist. Cannot add PayMongo columns.';
END
GO












