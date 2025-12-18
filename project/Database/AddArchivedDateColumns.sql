-- Add ArchivedDate column to Promotions and MembershipTypes tables
-- Run this script in your GymCRM_DB database

USE GymCRM_DB;
GO

-- Add ArchivedDate to Promotions table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Promotions' 
    AND COLUMN_NAME = 'ArchivedDate'
)
BEGIN
    ALTER TABLE Promotions
    ADD ArchivedDate DATETIME NULL;
    
    PRINT 'ArchivedDate column added successfully to Promotions table.';
END
ELSE
BEGIN
    PRINT 'ArchivedDate column already exists in Promotions table.';
END
GO

-- Add ArchivedDate to MembershipTypes table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MembershipTypes' 
    AND COLUMN_NAME = 'ArchivedDate'
)
BEGIN
    ALTER TABLE MembershipTypes
    ADD ArchivedDate DATETIME NULL;
    
    PRINT 'ArchivedDate column added successfully to MembershipTypes table.';
END
ELSE
BEGIN
    PRINT 'ArchivedDate column already exists in MembershipTypes table.';
END
GO

PRINT 'All ArchivedDate columns have been added successfully!';
GO

