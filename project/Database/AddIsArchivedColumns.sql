-- Add IsArchived column to Members, WalkIns, and MembershipTypes tables
-- Run this script in your GymCRM_DB database

USE GymCRM_DB;
GO

-- Add IsArchived to Members table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Members' 
    AND COLUMN_NAME = 'IsArchived'
)
BEGIN
    ALTER TABLE Members
    ADD IsArchived BIT NOT NULL DEFAULT 0;
    
    PRINT 'IsArchived column added successfully to Members table.';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in Members table.';
END
GO

-- Add IsArchived to WalkIns table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WalkIns' 
    AND COLUMN_NAME = 'IsArchived'
)
BEGIN
    ALTER TABLE WalkIns
    ADD IsArchived BIT NOT NULL DEFAULT 0;
    
    PRINT 'IsArchived column added successfully to WalkIns table.';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in WalkIns table.';
END
GO

-- Add IsArchived to MembershipTypes table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MembershipTypes' 
    AND COLUMN_NAME = 'IsArchived'
)
BEGIN
    ALTER TABLE MembershipTypes
    ADD IsArchived BIT NOT NULL DEFAULT 0;
    
    PRINT 'IsArchived column added successfully to MembershipTypes table.';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in MembershipTypes table.';
END
GO

PRINT 'All IsArchived columns have been added successfully!';
GO

