-- Add PaymentMethod column to WalkIns table
-- Run this script in your GymCRM_DB database

USE GymCRM_DB;
GO

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'WalkIns' AND COLUMN_NAME = 'PaymentMethod'
)
BEGIN
    ALTER TABLE WalkIns
    ADD PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash';

    PRINT 'PaymentMethod column added to WalkIns table.';
END
ELSE
BEGIN
    PRINT 'PaymentMethod column already exists in WalkIns table.';
END
GO

