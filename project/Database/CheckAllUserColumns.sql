-- Check ALL columns in Users table to find mismatches
USE GymCRM_DB;
GO

PRINT 'Checking all columns in Users table...';
PRINT '';

-- Get all columns
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;
GO

-- Check for columns that might cause issues
PRINT '';
PRINT 'Checking for potential issues...';

-- Check CreatedAt and UpdatedAt (DateTime in C#)
SELECT 
    'DateTime Columns' AS CheckType,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
AND COLUMN_NAME IN ('CreatedAt', 'UpdatedAt', 'LastLogin', 'LastPasswordChange');
GO

-- Check if any required columns are missing
PRINT '';
PRINT 'Testing if we can query Users table...';
BEGIN TRY
    SELECT TOP 1 
        UserID,
        Username,
        Password,
        RoleID,
        IsActive
    FROM Users;
    PRINT '✓ Simple SELECT works';
END TRY
BEGIN CATCH
    PRINT '✗ Simple SELECT FAILED:';
    PRINT ERROR_MESSAGE();
END CATCH
GO

-- Test with all columns
PRINT '';
PRINT 'Testing query with all columns...';
BEGIN TRY
    SELECT TOP 1 * FROM Users;
    PRINT '✓ SELECT * works';
END TRY
BEGIN CATCH
    PRINT '✗ SELECT * FAILED:';
    PRINT ERROR_MESSAGE();
END CATCH
GO

-- Test with Role join
PRINT '';
PRINT 'Testing query with Role join...';
BEGIN TRY
    SELECT TOP 1 
        u.UserID,
        u.Username,
        u.Password,
        u.RoleID,
        r.RoleName,
        u.IsActive
    FROM Users u
    LEFT JOIN Roles r ON u.RoleID = r.RoleID;
    PRINT '✓ Query with Role join works';
END TRY
BEGIN CATCH
    PRINT '✗ Query with Role join FAILED:';
    PRINT ERROR_MESSAGE();
END CATCH
GO

PRINT '';
PRINT '========================================';
PRINT 'Column check complete';
PRINT '========================================';
GO

