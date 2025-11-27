-- Comprehensive Database Verification for Admin Login
-- Run this to check if your database is correctly configured

USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'COMPREHENSIVE DATABASE VERIFICATION';
PRINT '========================================';
PRINT '';

-- 1. Check if Users table exists
PRINT '1. CHECKING USERS TABLE...';
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    PRINT '   ✓ Users table EXISTS';
    
    -- Check table structure
    SELECT 
        'Users Table Columns' AS Info,
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        CHARACTER_MAXIMUM_LENGTH
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users'
    ORDER BY ORDINAL_POSITION;
END
ELSE
BEGIN
    PRINT '   ✗ Users table DOES NOT EXIST';
    PRINT '   → CRITICAL: Users table is missing!';
END
GO

PRINT '';
PRINT '2. CHECKING ROLES TABLE...';
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles')
BEGIN
    PRINT '   ✓ Roles table EXISTS';
    
    -- List all roles
    SELECT 'Available Roles' AS Info, RoleID, RoleName FROM Roles ORDER BY RoleID;
END
ELSE
BEGIN
    PRINT '   ✗ Roles table DOES NOT EXIST';
    PRINT '   → CRITICAL: Roles table is missing!';
END
GO

PRINT '';
PRINT '3. CHECKING ADMIN USER...';
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    PRINT '   ✓ Admin user EXISTS';
    
    -- Get complete admin user info
    SELECT 
        'Admin User Details' AS Info,
        u.UserID,
        u.Username,
        u.Password,
        LEN(u.Password) AS PasswordLength,
        u.RoleID,
        r.RoleName,
        u.IsActive,
        CASE 
            WHEN u.IsActive IS NULL THEN 'NULL (Will be treated as ACTIVE)'
            WHEN u.IsActive = 1 THEN '1 (ACTIVE) ✓'
            WHEN u.IsActive = 0 THEN '0 (INACTIVE) ✗'
        END AS IsActiveStatus,
        u.CreatedAt,
        u.UpdatedAt
    FROM Users u
    LEFT JOIN Roles r ON u.RoleID = r.RoleID
    WHERE u.Username = 'admin';
    
    -- Check for common issues
    DECLARE @AdminPassword NVARCHAR(255);
    DECLARE @AdminRoleID INT;
    DECLARE @AdminIsActive BIT;
    
    SELECT 
        @AdminPassword = Password,
        @AdminRoleID = RoleID,
        @AdminIsActive = IsActive
    FROM Users
    WHERE Username = 'admin';
    
    PRINT '';
    PRINT '   ISSUE CHECK:';
    
    IF @AdminPassword IS NULL OR @AdminPassword = ''
    BEGIN
        PRINT '   ✗ PASSWORD is NULL or EMPTY';
    END
    ELSE IF @AdminPassword != 'adminpassword'
    BEGIN
        PRINT '   ⚠ PASSWORD is NOT "adminpassword"';
        PRINT '   → Current password: ' + @AdminPassword;
    END
    ELSE
    BEGIN
        PRINT '   ✓ PASSWORD is correct: "adminpassword"';
    END
    
    IF @AdminRoleID IS NULL
    BEGIN
        PRINT '   ✗ ROLEID is NULL';
    END
    ELSE IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleID = @AdminRoleID)
    BEGIN
        PRINT '   ✗ ROLEID ' + CAST(@AdminRoleID AS VARCHAR(10)) + ' does not exist in Roles table';
    END
    ELSE IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleID = @AdminRoleID AND RoleName = 'Admin')
    BEGIN
        DECLARE @ActualRoleName NVARCHAR(255);
        SELECT @ActualRoleName = RoleName FROM Roles WHERE RoleID = @AdminRoleID;
        PRINT '   ✗ ROLE is NOT "Admin" (Current: ' + @ActualRoleName + ')';
    END
    ELSE
    BEGIN
        PRINT '   ✓ ROLE is correctly set to "Admin"';
    END
    
    IF @AdminIsActive IS NULL
    BEGIN
        PRINT '   ⚠ ISACTIVE is NULL (Will be treated as ACTIVE by code)';
    END
    ELSE IF @AdminIsActive = 0
    BEGIN
        PRINT '   ✗ ISACTIVE is 0 (INACTIVE) - This will block login!';
    END
    ELSE
    BEGIN
        PRINT '   ✓ ISACTIVE is 1 (ACTIVE)';
    END
END
ELSE
BEGIN
    PRINT '   ✗ Admin user DOES NOT EXIST';
    PRINT '   → SOLUTION: Run FixAdminLogin.sql';
END
GO

PRINT '';
PRINT '4. CHECKING DATABASE CONNECTION STRING MATCH...';
-- Verify database name matches connection string
DECLARE @DBName NVARCHAR(255);
SELECT @DBName = DB_NAME();
PRINT '   Current database: ' + @DBName;
IF @DBName = 'GymCRM_DB'
BEGIN
    PRINT '   ✓ Database name matches expected: GymCRM_DB';
END
ELSE
BEGIN
    PRINT '   ⚠ Database name is: ' + @DBName;
    PRINT '   → Make sure your connection string uses the correct database name';
END
GO

PRINT '';
PRINT '5. COMPLETE USER LIST...';
SELECT 
    'All Users' AS Info,
    UserID,
    Username,
    CASE 
        WHEN Password IS NULL THEN 'NULL'
        WHEN Password = '' THEN 'EMPTY'
        ELSE 'SET'
    END AS PasswordStatus,
    RoleID,
    (SELECT RoleName FROM Roles WHERE RoleID = Users.RoleID) AS RoleName,
    IsActive,
    CreatedAt
FROM Users
ORDER BY UserID;
GO

PRINT '';
PRINT '6. TESTING LOGIN CREDENTIALS...';
PRINT '   Expected credentials:';
PRINT '   - Username: admin';
PRINT '   - Password: adminpassword';
PRINT '';
PRINT '   Verifying in database:';
IF EXISTS (
    SELECT 1 
    FROM Users u
    LEFT JOIN Roles r ON u.RoleID = r.RoleID
    WHERE u.Username = 'admin'
    AND u.Password = 'adminpassword'
    AND r.RoleName = 'Admin'
    AND (u.IsActive IS NULL OR u.IsActive = 1)
)
BEGIN
    PRINT '   ✓ LOGIN SHOULD WORK - All credentials match!';
END
ELSE
BEGIN
    PRINT '   ✗ LOGIN WILL FAIL - Issues found above';
    PRINT '';
    PRINT '   Quick Fix Query:';
    PRINT '   Run this to fix all issues:';
    PRINT '';
    PRINT '   USE GymCRM_DB;';
    PRINT '   GO';
    PRINT '   IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = ''Admin'')';
    PRINT '       INSERT INTO Roles (RoleName) VALUES (''Admin'');';
    PRINT '   UPDATE Users';
    PRINT '   SET Password = ''adminpassword'',';
    PRINT '       RoleID = (SELECT RoleID FROM Roles WHERE RoleName = ''Admin''),';
    PRINT '       IsActive = 1';
    PRINT '   WHERE Username = ''admin'';';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION COMPLETE';
PRINT '========================================';
GO

