-- Fix IsActive column to allow NULL values
-- This ensures the database matches the C# model

USE GymCRM_DB;
GO

PRINT 'Checking IsActive column...';
GO

-- Check current IsActive column definition
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive';
GO

-- If IsActive is NOT NULL, make it nullable
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'IsActive' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT 'Making IsActive column nullable...';
    ALTER TABLE Users
    ALTER COLUMN IsActive BIT NULL;
    PRINT '✓ IsActive column is now nullable';
END
ELSE
BEGIN
    PRINT '✓ IsActive column is already nullable';
END
GO

-- Verify the change
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive';
GO

PRINT '';
PRINT '========================================';
PRINT 'IsActive column fix complete';
PRINT '========================================';
GO

