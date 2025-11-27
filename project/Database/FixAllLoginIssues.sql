-- Complete Fix for All Login Issues
USE GymCRM_DB;
GO

PRINT '========================================';
PRINT 'FIXING ALL LOGIN ISSUES';
PRINT '========================================';
PRINT '';

-- Step 1: Ensure Admin role exists
PRINT 'Step 1: Checking Admin role...';
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Admin');
    PRINT '   ✓ Admin role created';
END
ELSE
BEGIN
    PRINT '   ✓ Admin role exists';
END
GO

-- Step 2: Make IsActive nullable
PRINT '';
PRINT 'Step 2: Making IsActive nullable...';
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'IsActive' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE Users ALTER COLUMN IsActive BIT NULL;
    PRINT '   ✓ IsActive is now nullable';
END
ELSE
BEGIN
    PRINT '   ✓ IsActive is already nullable';
END
GO

-- Step 3: Fix or create admin user
PRINT '';
PRINT 'Step 3: Fixing admin user...';
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    UPDATE Users
    SET 
        Password = 'adminpassword',
        RoleID = (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
        IsActive = 1,
        UpdatedAt = GETDATE()
    WHERE Username = 'admin';
    PRINT '   ✓ Admin user updated';
END
ELSE
BEGIN
    INSERT INTO Users (Username, Password, RoleID, IsActive, CreatedAt, UpdatedAt)
    VALUES (
        'admin',
        'adminpassword',
        (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
        1,
        GETDATE(),
        GETDATE()
    );
    PRINT '   ✓ Admin user created';
END
GO

-- Step 4: Verify everything
PRINT '';
PRINT 'Step 4: Verifying fix...';
SELECT 
    'VERIFICATION' AS Info,
    u.Username,
    u.Password,
    r.RoleName,
    u.IsActive,
    CASE 
        WHEN u.Password = 'adminpassword' 
         AND r.RoleName = 'Admin' 
         AND (u.IsActive IS NULL OR u.IsActive = 1)
        THEN '✓ ALL CORRECT'
        ELSE '✗ ISSUES FOUND'
    END AS Status
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

PRINT '';
PRINT '========================================';
PRINT 'FIX COMPLETE';
PRINT '========================================';
PRINT 'Login Credentials:';
PRINT '  Username: admin';
PRINT '  Password: adminpassword';
PRINT '========================================';
GO

