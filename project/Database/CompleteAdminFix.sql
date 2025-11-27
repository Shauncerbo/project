-- Complete Admin Fix - Fixes ALL possible issues
-- Run this to ensure admin can login

USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'COMPLETE ADMIN FIX';
PRINT '========================================';
PRINT '';

-- Step 1: Ensure Admin role exists
PRINT 'Step 1: Checking Admin role...';
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO Roles (RoleName)
    VALUES ('Admin');
    PRINT '   ✓ Admin role CREATED';
END
ELSE
BEGIN
    PRINT '   ✓ Admin role already exists';
END
GO

-- Step 2: Get Admin RoleID
DECLARE @AdminRoleID INT;
SELECT @AdminRoleID = RoleID FROM Roles WHERE RoleName = 'Admin';
PRINT '   Admin RoleID: ' + CAST(@AdminRoleID AS VARCHAR(10));
GO

-- Step 3: Fix or create admin user
PRINT '';
PRINT 'Step 2: Fixing admin user...';
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    PRINT '   Admin user exists - UPDATING...';
    
    UPDATE Users
    SET 
        Password = 'adminpassword',
        RoleID = (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
        IsActive = 1,
        UpdatedAt = GETDATE()
    WHERE Username = 'admin';
    
    PRINT '   ✓ Admin user UPDATED';
END
ELSE
BEGIN
    PRINT '   Admin user does not exist - CREATING...';
    
    INSERT INTO Users (Username, Password, RoleID, IsActive, CreatedAt, UpdatedAt)
    VALUES (
        'admin',
        'adminpassword',
        (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
        1,
        GETDATE(),
        GETDATE()
    );
    
    PRINT '   ✓ Admin user CREATED';
END
GO

-- Step 4: Verify the fix
PRINT '';
PRINT 'Step 3: Verifying fix...';
SELECT 
    'VERIFICATION - Admin User' AS Info,
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    CASE 
        WHEN u.IsActive = 1 THEN 'ACTIVE ✓'
        WHEN u.IsActive IS NULL THEN 'NULL (treated as active) ✓'
        ELSE 'INACTIVE ✗'
    END AS Status
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

-- Step 5: Final check
PRINT '';
PRINT 'Step 4: Final login test...';
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
    PRINT '   ✓✓✓ ALL CHECKS PASSED - ADMIN CAN LOGIN ✓✓✓';
    PRINT '';
    PRINT '   Login Credentials:';
    PRINT '   Username: admin';
    PRINT '   Password: adminpassword';
END
ELSE
BEGIN
    PRINT '   ✗✗✗ ISSUES FOUND - Check output above ✗✗✗';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'FIX COMPLETE';
PRINT '========================================';
GO

