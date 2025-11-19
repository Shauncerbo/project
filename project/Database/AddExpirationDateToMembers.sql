-- Add ExpirationDate column to Members table if it doesn't exist
-- Run this script in your GymCRM_DB database

USE GymCRM_DB;
GO

-- Check if column exists, if not, add it
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Members' 
    AND COLUMN_NAME = 'ExpirationDate'
)
BEGIN
    ALTER TABLE Members
    ADD ExpirationDate DATETIME NULL;
    
    PRINT 'ExpirationDate column added successfully to Members table.';
END
ELSE
BEGIN
    PRINT 'ExpirationDate column already exists in Members table.';
END
GO

-- Now calculate expiration dates for existing members based on their JoinDate and MembershipType
-- This will backfill expiration dates for existing records
-- Only runs if the column exists (which it should after the above statement)
IF EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Members' 
    AND COLUMN_NAME = 'ExpirationDate'
)
BEGIN
    UPDATE m
    SET m.ExpirationDate = DATEADD(DAY, mt.DurationInDays, m.JoinDate)
    FROM Members m
    INNER JOIN MembershipTypes mt ON m.MembershipTypeID = mt.MembershipTypeID
    WHERE m.ExpirationDate IS NULL;
    
    PRINT 'Expiration dates calculated for existing members.';
END
ELSE
BEGIN
    PRINT 'ERROR: ExpirationDate column does not exist. Cannot calculate expiration dates.';
END
GO

