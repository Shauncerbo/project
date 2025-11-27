-- Comprehensive Admin Login Diagnostic Query
-- Run this to check all possible issues preventing admin login

USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'ADMIN LOGIN DIAGNOSTIC REPORT';
PRINT '========================================';
PRINT '';

-- 1. Check if Admin user exists
PRINT '1. CHECKING IF ADMIN USER EXISTS...';
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    PRINT '   ✓ Admin user EXISTS in database';
    
    -- Get admin user details
    SELECT 
        'Admin User Details' AS CheckType,
        UserID,
        Username,
        LEN(Password) AS PasswordLength,
        CASE 
            WHEN Password IS NULL THEN 'NULL (EMPTY)'
            WHEN Password = '' THEN 'EMPTY STRING'
            ELSE 'HAS PASSWORD'
        END AS PasswordStatus,
        RoleID,
        IsActive,
        CreatedAt,
        UpdatedAt
    FROM Users
    WHERE Username = 'admin';
END
ELSE
BEGIN
    PRINT '   ✗ Admin user DOES NOT EXIST in database';
    PRINT '   → SOLUTION: Run FixAdminLogin.sql to create admin user';
END
GO

PRINT '';
PRINT '2. CHECKING ADMIN ROLE ASSIGNMENT...';
-- Check if Admin role exists and is assigned
DECLARE @AdminRoleID INT;
SELECT @AdminRoleID = RoleID FROM Roles WHERE RoleName = 'Admin';

IF @AdminRoleID IS NOT NULL
BEGIN
    PRINT '   ✓ Admin role EXISTS (RoleID: ' + CAST(@AdminRoleID AS VARCHAR(10)) + ')';
    
    -- Check if admin user has Admin role
    IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin' AND RoleID = @AdminRoleID)
    BEGIN
        PRINT '   ✓ Admin user HAS Admin role assigned';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Admin user DOES NOT have Admin role assigned';
        PRINT '   → Current RoleID: ' + CAST((SELECT RoleID FROM Users WHERE Username = 'admin') AS VARCHAR(10));
        PRINT '   → SOLUTION: Run FixAdminLogin.sql to assign Admin role';
    END
END
ELSE
BEGIN
    PRINT '   ✗ Admin role DOES NOT EXIST';
    PRINT '   → SOLUTION: Run FixAdminLogin.sql to create Admin role';
END
GO

PRINT '';
PRINT '3. CHECKING USER STATUS (IsActive)...';
-- Check if user is active
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    DECLARE @IsActive BIT;
    SELECT @IsActive = IsActive FROM Users WHERE Username = 'admin';
    
    IF @IsActive = 1
    BEGIN
        PRINT '   ✓ Admin user IS ACTIVE';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Admin user IS INACTIVE (IsActive = 0)';
        PRINT '   → SOLUTION: Run FixAdminLogin.sql to activate user';
    END
END
GO

PRINT '';
PRINT '4. CHECKING PASSWORD...';
-- Check password details
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    DECLARE @Password NVARCHAR(255);
    SELECT @Password = Password FROM Users WHERE Username = 'admin';
    
    IF @Password IS NULL OR @Password = ''
    BEGIN
        PRINT '   ✗ Admin password is NULL or EMPTY';
        PRINT '   → SOLUTION: Run FixAdminLogin.sql to set password';
    END
    ELSE IF @Password = 'adminpassword'
    BEGIN
        PRINT '   ✓ Admin password is set to: adminpassword';
        PRINT '   → Expected login: Username = "admin", Password = "adminpassword"';
    END
    ELSE
    BEGIN
        PRINT '   ⚠ Admin password is set but NOT "adminpassword"';
        PRINT '   → Current password length: ' + CAST(LEN(@Password) AS VARCHAR(10));
        PRINT '   → SOLUTION: Run FixAdminLogin.sql to reset password to "adminpassword"';
    END
END
GO

PRINT '';
PRINT '5. COMPLETE USER DETAILS WITH ROLE...';
-- Show complete user information
SELECT 
    'Complete Admin User Info' AS ReportSection,
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    CASE 
        WHEN u.IsActive = 1 THEN 'ACTIVE ✓'
        ELSE 'INACTIVE ✗'
    END AS Status,
    u.CreatedAt,
    u.UpdatedAt
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

PRINT '';
PRINT '6. CHECKING ALL ROLES IN DATABASE...';
-- List all roles
SELECT 
    'Available Roles' AS CheckType,
    RoleID,
    RoleName
FROM Roles
ORDER BY RoleID;
GO

PRINT '';
PRINT '7. CHECKING ALL USERS IN DATABASE...';
-- List all users
SELECT 
    'All Users' AS CheckType,
    UserID,
    Username,
    RoleID,
    IsActive,
    CreatedAt
FROM Users
ORDER BY UserID;
GO

PRINT '';
PRINT '========================================';
PRINT 'DIAGNOSTIC COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'EXPECTED LOGIN CREDENTIALS:';
PRINT '  Username: admin';
PRINT '  Password: adminpassword';
PRINT '';
PRINT 'If any issues were found above, run FixAdminLogin.sql to fix them.';
PRINT '========================================';
GO

