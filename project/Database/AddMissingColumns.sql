-- Add Description and ArchivedDate columns to MembershipTypes and Promotions tables
-- Run this script in your MonsterASP database (db32884) to fix sync errors

-- ============================================
-- For MONSTERASP DATABASE (db32884)
-- ============================================
USE db32884;
GO

-- Add Description to MembershipTypes table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MembershipTypes' 
    AND COLUMN_NAME = 'Description'
)
BEGIN
    ALTER TABLE MembershipTypes
    ADD Description NVARCHAR(MAX) NULL;
    
    PRINT 'Description column added successfully to MembershipTypes table (MonsterASP).';
END
ELSE
BEGIN
    PRINT 'Description column already exists in MembershipTypes table (MonsterASP).';
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
    
    PRINT 'ArchivedDate column added successfully to MembershipTypes table (MonsterASP).';
END
ELSE
BEGIN
    PRINT 'ArchivedDate column already exists in MembershipTypes table (MonsterASP).';
END
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
    
    PRINT 'ArchivedDate column added successfully to Promotions table (MonsterASP).';
END
ELSE
BEGIN
    PRINT 'ArchivedDate column already exists in Promotions table (MonsterASP).';
END
GO

PRINT 'MonsterASP database migration completed successfully!';
GO

