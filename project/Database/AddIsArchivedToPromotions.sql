-- Add IsArchived column to Promotions table
-- Run this script in your GymCRM_DB database

USE GymCRM_DB;
GO

-- Add IsArchived to Promotions table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Promotions' 
    AND COLUMN_NAME = 'IsArchived'
)
BEGIN
    ALTER TABLE Promotions
    ADD IsArchived BIT NOT NULL DEFAULT 0;
    
    PRINT 'IsArchived column added successfully to Promotions table.';
END
ELSE
BEGIN
    PRINT 'IsArchived column already exists in Promotions table.';
END
GO

PRINT 'IsArchived column has been added successfully to Promotions table!';
GO


