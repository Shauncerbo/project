-- Add StudentIDImage column to Members table
-- Run this script in both LOCAL (GymCRM_DB) and ONLINE (db32884) databases

-- ============================================
-- For LOCAL DATABASE (GymCRM_DB)
-- ============================================
USE GymCRM_DB;
GO

-- Add StudentIDImage to Members table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Members' 
    AND COLUMN_NAME = 'StudentIDImage'
)
BEGIN
    ALTER TABLE Members
    ADD StudentIDImage VARBINARY(MAX) NULL;
    
    PRINT 'StudentIDImage column added successfully to Members table (Local).';
END
ELSE
BEGIN
    PRINT 'StudentIDImage column already exists in Members table (Local).';
END
GO

-- ============================================
-- For ONLINE DATABASE (db32884 - MonsterASP)
-- ============================================
USE db32884;
GO

-- Add StudentIDImage to Members table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Members' 
    AND COLUMN_NAME = 'StudentIDImage'
)
BEGIN
    ALTER TABLE Members
    ADD StudentIDImage VARBINARY(MAX) NULL;
    
    PRINT 'StudentIDImage column added successfully to Members table (Online).';
END
ELSE
BEGIN
    PRINT 'StudentIDImage column already exists in Members table (Online).';
END
GO

PRINT 'StudentIDImage column migration completed successfully for both databases!';
GO

