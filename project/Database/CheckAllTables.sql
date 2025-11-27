-- Complete Database Structure Check
-- This checks ALL tables and their structure

USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'COMPLETE DATABASE STRUCTURE CHECK';
PRINT '========================================';
PRINT '';

-- 1. List all tables
PRINT '1. ALL TABLES IN DATABASE:';
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- 2. Check Users table structure
PRINT '';
PRINT '2. USERS TABLE STRUCTURE:';
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

-- 3. Check Roles table structure
PRINT '';
PRINT '3. ROLES TABLE STRUCTURE:';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Roles'
ORDER BY ORDINAL_POSITION;
GO

-- 4. Check Users table data
PRINT '';
PRINT '4. USERS TABLE DATA:';
SELECT 
    UserID,
    Username,
    Password,
    RoleID,
    IsActive,
    CreatedAt,
    UpdatedAt,
    TrainerID
FROM Users
ORDER BY UserID;
GO

-- 5. Check Roles table data
PRINT '';
PRINT '5. ROLES TABLE DATA:';
SELECT 
    RoleID,
    RoleName
FROM Roles
ORDER BY RoleID;
GO

-- 6. Check Admin user with role
PRINT '';
PRINT '6. ADMIN USER WITH ROLE:';
SELECT 
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    u.CreatedAt,
    u.UpdatedAt,
    u.TrainerID
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

-- 7. Check for potential issues
PRINT '';
PRINT '7. POTENTIAL ISSUES CHECK:';

-- Check if IsActive is nullable
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'IsActive' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT '   ✗ IsActive is NOT NULL (should be nullable)';
END
ELSE
BEGIN
    PRINT '   ✓ IsActive is nullable';
END

-- Check if Admin role exists
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    PRINT '   ✗ Admin role does NOT exist';
END
ELSE
BEGIN
    PRINT '   ✓ Admin role exists';
END

-- Check if admin user exists
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    PRINT '   ✗ Admin user does NOT exist';
END
ELSE
BEGIN
    PRINT '   ✓ Admin user exists';
    
    -- Check admin password
    DECLARE @AdminPassword NVARCHAR(255);
    SELECT @AdminPassword = Password FROM Users WHERE Username = 'admin';
    IF @AdminPassword != 'adminpassword'
    BEGIN
        PRINT '   ✗ Admin password is NOT "adminpassword"';
    END
    ELSE
    BEGIN
        PRINT '   ✓ Admin password is correct';
    END
    
    -- Check admin role assignment
    IF NOT EXISTS (
        SELECT 1 FROM Users u
        INNER JOIN Roles r ON u.RoleID = r.RoleID
        WHERE u.Username = 'admin' AND r.RoleName = 'Admin'
    )
    BEGIN
        PRINT '   ✗ Admin user does NOT have Admin role assigned';
    END
    ELSE
    BEGIN
        PRINT '   ✓ Admin user has Admin role assigned';
    END
    
    -- Check IsActive value
    DECLARE @IsActive BIT;
    SELECT @IsActive = IsActive FROM Users WHERE Username = 'admin';
    IF @IsActive = 0
    BEGIN
        PRINT '   ✗ Admin user IsActive is 0 (INACTIVE)';
    END
    ELSE IF @IsActive IS NULL
    BEGIN
        PRINT '   ⚠ Admin user IsActive is NULL (will be treated as active)';
    END
    ELSE
    BEGIN
        PRINT '   ✓ Admin user IsActive is 1 (ACTIVE)';
    END
END

-- Check TrainerID column
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'TrainerID'
)
BEGIN
    PRINT '   ⚠ TrainerID column exists in Users table';
END
ELSE
BEGIN
    PRINT '   ✓ TrainerID column does NOT exist';
END
GO

-- 8. Test login query simulation
PRINT '';
PRINT '8. TESTING LOGIN QUERY SIMULATION:';
DECLARE @TestUsername NVARCHAR(50) = 'admin';
DECLARE @TestPassword NVARCHAR(255) = 'adminpassword';

BEGIN TRY
    SELECT 
        'Login Test Result' AS TestType,
        u.UserID,
        u.Username,
        CASE 
            WHEN u.Password = @TestPassword THEN 'PASSWORD MATCH ✓'
            ELSE 'PASSWORD MISMATCH ✗'
        END AS PasswordCheck,
        r.RoleName,
        CASE 
            WHEN u.IsActive IS NULL THEN 'NULL (treated as active) ✓'
            WHEN u.IsActive = 1 THEN 'ACTIVE ✓'
            WHEN u.IsActive = 0 THEN 'INACTIVE ✗'
        END AS ActiveStatus,
        CASE 
            WHEN u.Password = @TestPassword 
             AND r.RoleName = 'Admin' 
             AND (u.IsActive IS NULL OR u.IsActive = 1)
            THEN 'LOGIN WILL SUCCEED ✓✓✓'
            ELSE 'LOGIN WILL FAIL ✗✗✗'
        END AS FinalVerdict
    FROM Users u
    LEFT JOIN Roles r ON u.RoleID = r.RoleID
    WHERE u.Username = @TestUsername;
    
    PRINT '   ✓ Query executed successfully';
END TRY
BEGIN CATCH
    PRINT '   ✗ Query FAILED:';
    PRINT '   ' + ERROR_MESSAGE();
END CATCH
GO

-- 9. List all other tables
PRINT '';
PRINT '9. ALL OTHER TABLES:';
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
AND TABLE_NAME NOT IN ('Users', 'Roles')
ORDER BY TABLE_NAME;
GO

PRINT '';
PRINT '========================================';
PRINT 'CHECK COMPLETE';
PRINT '========================================';
GO

