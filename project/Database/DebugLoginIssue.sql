-- Debug Login Issue - Check Everything
USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'LOGIN DEBUGGING - STEP BY STEP';
PRINT '========================================';
PRINT '';

-- Step 1: Check exact password value (including hidden characters)
PRINT 'Step 1: Checking password value...';
SELECT 
    'Password Analysis' AS CheckType,
    Username,
    Password,
    LEN(Password) AS PasswordLength,
    ASCII(SUBSTRING(Password, 1, 1)) AS FirstCharASCII,
    ASCII(SUBSTRING(Password, LEN(Password), 1)) AS LastCharASCII,
    CASE 
        WHEN Password = 'adminpassword' THEN 'MATCHES ✓'
        ELSE 'DOES NOT MATCH ✗'
    END AS PasswordMatch
FROM Users
WHERE Username = 'admin';
GO

-- Step 2: Check for trailing/leading spaces
PRINT '';
PRINT 'Step 2: Checking for spaces...';
SELECT 
    'Space Check' AS CheckType,
    Username,
    '[' + Password + ']' AS PasswordWithBrackets,
    LEN(Password) AS Length,
    LEN(LTRIM(RTRIM(Password))) AS TrimmedLength,
    CASE 
        WHEN LEN(Password) = LEN(LTRIM(RTRIM(Password))) THEN 'No spaces ✓'
        ELSE 'Has spaces ✗'
    END AS SpaceCheck
FROM Users
WHERE Username = 'admin';
GO

-- Step 3: Test exact comparison
PRINT '';
PRINT 'Step 3: Testing exact password comparison...';
DECLARE @DBPassword NVARCHAR(255);
SELECT @DBPassword = Password FROM Users WHERE Username = 'admin';

IF @DBPassword = 'adminpassword'
BEGIN
    PRINT '   ✓ Direct comparison: PASS';
END
ELSE
BEGIN
    PRINT '   ✗ Direct comparison: FAIL';
    PRINT '   → This is the problem!';
END

IF LTRIM(RTRIM(@DBPassword)) = 'adminpassword'
BEGIN
    PRINT '   ✓ Trimmed comparison: PASS';
END
ELSE
BEGIN
    PRINT '   ✗ Trimmed comparison: FAIL';
END
GO

-- Step 4: Check Role loading
PRINT '';
PRINT 'Step 4: Checking Role loading...';
SELECT 
    'Role Check' AS CheckType,
    u.Username,
    u.RoleID,
    r.RoleName,
    CASE 
        WHEN r.RoleName = 'Admin' THEN 'CORRECT ✓'
        WHEN r.RoleName IS NULL THEN 'NULL ✗'
        ELSE 'WRONG ✗'
    END AS RoleStatus
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

-- Step 5: Simulate the exact EF Core query
PRINT '';
PRINT 'Step 5: Simulating EF Core query...';
-- This is what EF Core does: SELECT * FROM Users INNER JOIN Roles ON Users.RoleID = Roles.RoleID
SELECT 
    'EF Core Simulation' AS CheckType,
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive
FROM Users u
INNER JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

-- Step 6: Final verdict
PRINT '';
PRINT 'Step 6: Final Login Test...';
DECLARE @CanLogin BIT = 0;
DECLARE @Issues NVARCHAR(MAX) = '';

-- Check password
DECLARE @Pwd NVARCHAR(255);
SELECT @Pwd = Password FROM Users WHERE Username = 'admin';
IF @Pwd != 'adminpassword'
BEGIN
    SET @Issues = @Issues + 'Password mismatch. ';
END

-- Check IsActive
DECLARE @Active BIT;
SELECT @Active = IsActive FROM Users WHERE Username = 'admin';
IF @Active = 0
BEGIN
    SET @Issues = @Issues + 'User is inactive. ';
END

-- Check Role
IF NOT EXISTS (
    SELECT 1 FROM Users u
    INNER JOIN Roles r ON u.RoleID = r.RoleID
    WHERE u.Username = 'admin' AND r.RoleName = 'Admin'
)
BEGIN
    SET @Issues = @Issues + 'Role not assigned correctly. ';
END

IF @Issues = ''
BEGIN
    SET @CanLogin = 1;
    PRINT '   ✓✓✓ ALL CHECKS PASSED - LOGIN SHOULD WORK ✓✓✓';
END
ELSE
BEGIN
    PRINT '   ✗✗✗ ISSUES FOUND:';
    PRINT '   ' + @Issues;
END
GO

PRINT '';
PRINT '========================================';
PRINT 'DEBUG COMPLETE';
PRINT '========================================';
GO

