-- Fix DateTime columns to allow NULL if needed
USE GymCRM_DB;
GO

PRINT 'Checking DateTime columns...';
GO

-- Check current DateTime column definitions
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
AND COLUMN_NAME IN ('CreatedAt', 'UpdatedAt', 'LastPasswordChange', 'LastLogin')
ORDER BY COLUMN_NAME;
GO

-- Make CreatedAt nullable if it's not
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'CreatedAt' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT 'Making CreatedAt nullable...';
    ALTER TABLE Users
    ALTER COLUMN CreatedAt DATETIME NULL;
    PRINT '✓ CreatedAt is now nullable';
END
ELSE
BEGIN
    PRINT '✓ CreatedAt is already nullable or doesn''t exist';
END
GO

-- Make UpdatedAt nullable if it's not
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'UpdatedAt' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT 'Making UpdatedAt nullable...';
    ALTER TABLE Users
    ALTER COLUMN UpdatedAt DATETIME NULL;
    PRINT '✓ UpdatedAt is now nullable';
END
ELSE
BEGIN
    PRINT '✓ UpdatedAt is already nullable or doesn''t exist';
END
GO

-- Verify all DateTime columns
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
AND COLUMN_NAME IN ('CreatedAt', 'UpdatedAt', 'LastPasswordChange', 'LastLogin', 'IsActive')
ORDER BY COLUMN_NAME;
GO

PRINT '';
PRINT '========================================';
PRINT 'DateTime columns check complete';
PRINT '========================================';
GO

