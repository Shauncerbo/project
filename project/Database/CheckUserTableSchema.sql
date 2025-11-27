-- Check Users table schema to find the issue
USE GymCRM_DB;
GO

PRINT 'Checking Users table schema...';
PRINT '';

-- Check if table exists
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    PRINT '✓ Users table EXISTS';
    
    -- Get all columns and their properties
    SELECT 
        'Users Table Schema' AS Info,
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        CHARACTER_MAXIMUM_LENGTH,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users'
    ORDER BY ORDINAL_POSITION;
END
ELSE
BEGIN
    PRINT '✗ Users table DOES NOT EXIST';
END
GO

-- Check IsActive column specifically
PRINT '';
PRINT 'Checking IsActive column...';
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive'
)
BEGIN
    SELECT 
        'IsActive Column' AS Info,
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive';
    
    -- Check actual values
    PRINT '';
    PRINT 'IsActive values in database:';
    SELECT 
        Username,
        IsActive,
        CASE 
            WHEN IsActive IS NULL THEN 'NULL'
            WHEN IsActive = 1 THEN '1 (True)'
            WHEN IsActive = 0 THEN '0 (False)'
        END AS IsActiveStatus
    FROM Users;
END
ELSE
BEGIN
    PRINT '✗ IsActive column DOES NOT EXIST';
END
GO

-- Test simple query
PRINT '';
PRINT 'Testing simple query...';
BEGIN TRY
    SELECT TOP 1 UserID, Username FROM Users;
    PRINT '✓ Simple query works';
END TRY
BEGIN CATCH
    PRINT '✗ Simple query FAILED:';
    PRINT ERROR_MESSAGE();
END CATCH
GO

-- Test query with Role join
PRINT '';
PRINT 'Testing query with Role join...';
BEGIN TRY
    SELECT TOP 1 
        u.UserID, 
        u.Username, 
        u.RoleID,
        r.RoleName
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
PRINT 'Schema check complete';
PRINT '========================================';
GO

