-- Complete Login Setup Verification
-- Run this script to verify everything is correct for admin login

USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'LOGIN SETUP VERIFICATION';
PRINT '========================================';
PRINT '';

-- 1. Check if Users table exists
PRINT '1. Checking Users table...';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    PRINT '   ✓ Users table exists';
END
ELSE
BEGIN
    PRINT '   ✗ Users table DOES NOT EXIST!';
    RETURN;
END
GO

-- 2. Check Users table columns
PRINT '';
PRINT '2. Users table columns:';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;
GO

-- 3. Check if Roles table exists
PRINT '';
PRINT '3. Checking Roles table...';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles')
BEGIN
    PRINT '   ✓ Roles table exists';
END
ELSE
BEGIN
    PRINT '   ✗ Roles table DOES NOT EXIST!';
    RETURN;
END
GO

-- 4. Check all users
PRINT '';
PRINT '4. All users in database:';
SELECT 
    UserID,
    Username,
    Password,
    RoleID,
    IsActive,
    CreatedAt,
    UpdatedAt,
    TrainerID
FROM Users;
GO

-- 5. Check admin user specifically
PRINT '';
PRINT '5. Admin user details:';
SELECT 
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    u.TrainerID,
    LEN(u.Username) AS UsernameLength,
    LEN(u.Password) AS PasswordLength
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE LOWER(u.Username) = 'admin';
GO

-- 6. Check all roles
PRINT '';
PRINT '6. All roles in database:';
SELECT RoleID, RoleName FROM Roles;
GO

-- 7. Verify admin user setup
PRINT '';
PRINT '7. Admin user verification:';
DECLARE @AdminUsername NVARCHAR(255) = 'admin';
DECLARE @AdminPassword NVARCHAR(255) = 'adminpassword';
DECLARE @AdminExists BIT = 0;
DECLARE @AdminPasswordMatch BIT = 0;
DECLARE @AdminIsActive BIT = 0;
DECLARE @AdminHasRole BIT = 0;

-- Check if admin exists
IF EXISTS (SELECT 1 FROM Users WHERE LOWER(Username) = LOWER(@AdminUsername))
BEGIN
    SET @AdminExists = 1;
    PRINT '   ✓ Admin user exists';
    
    -- Check password
    IF EXISTS (SELECT 1 FROM Users WHERE LOWER(Username) = LOWER(@AdminUsername) AND Password = @AdminPassword)
    BEGIN
        SET @AdminPasswordMatch = 1;
        PRINT '   ✓ Admin password matches';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Admin password DOES NOT MATCH!';
        PRINT '   Current password in DB:';
        SELECT Password, LEN(Password) AS PasswordLength FROM Users WHERE LOWER(Username) = LOWER(@AdminUsername);
    END
    
    -- Check IsActive
    DECLARE @IsActiveValue BIT;
    SELECT @IsActiveValue = IsActive FROM Users WHERE LOWER(Username) = LOWER(@AdminUsername);
    IF @IsActiveValue IS NULL OR @IsActiveValue = 1
    BEGIN
        SET @AdminIsActive = 1;
        PRINT '   ✓ Admin is active (IsActive = ' + ISNULL(CAST(@IsActiveValue AS VARCHAR), 'NULL') + ')';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Admin is NOT active (IsActive = 0)';
    END
    
    -- Check role
    IF EXISTS (SELECT 1 FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE LOWER(u.Username) = LOWER(@AdminUsername) AND LOWER(r.RoleName) = 'admin')
    BEGIN
        SET @AdminHasRole = 1;
        PRINT '   ✓ Admin has Admin role';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Admin DOES NOT have Admin role!';
        PRINT '   Current RoleID and RoleName:';
        SELECT u.RoleID, r.RoleName FROM Users u LEFT JOIN Roles r ON u.RoleID = r.RoleID WHERE LOWER(u.Username) = LOWER(@AdminUsername);
    END
END
ELSE
BEGIN
    PRINT '   ✗ Admin user DOES NOT EXIST!';
END

-- Summary
PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION SUMMARY:';
PRINT '========================================';
PRINT 'Admin exists: ' + CASE WHEN @AdminExists = 1 THEN 'YES' ELSE 'NO' END;
PRINT 'Password matches: ' + CASE WHEN @AdminPasswordMatch = 1 THEN 'YES' ELSE 'NO' END;
PRINT 'Admin is active: ' + CASE WHEN @AdminIsActive = 1 THEN 'YES' ELSE 'NO' END;
PRINT 'Has Admin role: ' + CASE WHEN @AdminHasRole = 1 THEN 'YES' ELSE 'NO' END;
PRINT '';

IF @AdminExists = 1 AND @AdminPasswordMatch = 1 AND @AdminIsActive = 1 AND @AdminHasRole = 1
BEGIN
    PRINT '✓✓✓ ALL CHECKS PASSED - Admin login should work! ✓✓✓';
END
ELSE
BEGIN
    PRINT '✗✗✗ SOME CHECKS FAILED - Admin login will NOT work! ✗✗✗';
    PRINT '';
    PRINT 'To fix, run this script:';
    PRINT '-- Fix admin user';
    PRINT 'UPDATE Users SET Password = ''adminpassword'', IsActive = 1 WHERE LOWER(Username) = ''admin'';';
    PRINT '';
    PRINT '-- Ensure Admin role exists';
    PRINT 'IF NOT EXISTS (SELECT 1 FROM Roles WHERE LOWER(RoleName) = ''admin'')';
    PRINT 'BEGIN';
    PRINT '    INSERT INTO Roles (RoleName) VALUES (''Admin'');';
    PRINT 'END';
    PRINT '';
    PRINT '-- Link admin to Admin role';
    PRINT 'UPDATE Users SET RoleID = (SELECT RoleID FROM Roles WHERE LOWER(RoleName) = ''admin'') WHERE LOWER(Username) = ''admin'';';
END
GO

