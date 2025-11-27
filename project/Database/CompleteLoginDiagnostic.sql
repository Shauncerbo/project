-- Complete Login Diagnostic - Check Everything
USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'COMPLETE LOGIN DIAGNOSTIC';
PRINT '========================================';
PRINT '';

-- 1. Check Users table structure
PRINT '1. USERS TABLE STRUCTURE:';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;
GO

-- 2. Check Admin user
PRINT '';
PRINT '2. ADMIN USER CHECK:';
SELECT 
    'Admin User' AS CheckType,
    UserID,
    Username,
    Password,
    LEN(Password) AS PasswordLength,
    RoleID,
    IsActive,
    CreatedAt,
    UpdatedAt,
    TrainerID
FROM Users
WHERE Username = 'admin';
GO

-- 3. Check Admin role
PRINT '';
PRINT '3. ADMIN ROLE CHECK:';
SELECT 
    'Admin Role' AS CheckType,
    r.RoleID,
    r.RoleName,
    u.Username,
    u.RoleID AS UserRoleID
FROM Roles r
LEFT JOIN Users u ON u.RoleID = r.RoleID AND u.Username = 'admin'
WHERE r.RoleName = 'Admin';
GO

-- 4. Test exact login query
PRINT '';
PRINT '4. TESTING EXACT LOGIN QUERY:';
DECLARE @TestUsername NVARCHAR(50) = 'admin';
DECLARE @TestPassword NVARCHAR(255) = 'adminpassword';

SELECT 
    'Login Test' AS TestType,
    u.UserID,
    u.Username,
    u.Password,
    CASE 
        WHEN u.Password = @TestPassword THEN 'MATCH ✓'
        ELSE 'NO MATCH ✗'
    END AS PasswordMatch,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    CASE 
        WHEN u.IsActive IS NULL THEN 'NULL (treated as active)'
        WHEN u.IsActive = 1 THEN '1 (ACTIVE) ✓'
        WHEN u.IsActive = 0 THEN '0 (INACTIVE) ✗'
    END AS IsActiveStatus
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = @TestUsername;
GO

-- 5. Check for TrainerID column issue
PRINT '';
PRINT '5. TRAINERID COLUMN CHECK:';
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'TrainerID'
)
BEGIN
    PRINT '   ⚠ TrainerID column EXISTS in database';
    SELECT 
        'TrainerID Values' AS Info,
        Username,
        TrainerID
    FROM Users
    WHERE Username = 'admin';
END
ELSE
BEGIN
    PRINT '   ✓ TrainerID column does NOT exist (good)';
END
GO

-- 6. Final verdict
PRINT '';
PRINT '6. FINAL VERDICT:';
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
    PRINT '   ✓✓✓ ALL CHECKS PASSED - LOGIN SHOULD WORK ✓✓✓';
    PRINT '';
    PRINT '   Expected Login:';
    PRINT '   Username: admin';
    PRINT '   Password: adminpassword';
END
ELSE
BEGIN
    PRINT '   ✗✗✗ ISSUES FOUND - Check above for details ✗✗✗';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'DIAGNOSTIC COMPLETE';
PRINT '========================================';
GO

