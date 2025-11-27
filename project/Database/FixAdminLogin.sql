-- Fix Admin Login Issues
-- Run this script to ensure admin user can login

USE GymCRM_DB;
GO

-- Step 1: Ensure Admin role exists
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO Roles (RoleName)
    VALUES ('Admin');
    PRINT 'Admin role created.';
END
ELSE
BEGIN
    PRINT 'Admin role already exists.';
END
GO

-- Step 2: Get Admin RoleID
DECLARE @AdminRoleID INT;
SELECT @AdminRoleID = RoleID FROM Roles WHERE RoleName = 'Admin';
PRINT 'Admin RoleID: ' + CAST(@AdminRoleID AS VARCHAR(10));
GO

-- Step 3: Check if admin user exists
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    PRINT 'Admin user exists. Updating...';
    
    -- Update admin user to ensure it's active and has correct password
    UPDATE Users
    SET 
        Password = 'adminpassword',
        RoleID = (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
        IsActive = 1,
        UpdatedAt = GETDATE()
    WHERE Username = 'admin';
    
    PRINT 'Admin user updated successfully.';
END
ELSE
BEGIN
    PRINT 'Admin user does not exist. Creating...';
    
    -- Create admin user
    INSERT INTO Users (Username, Password, RoleID, IsActive, CreatedAt, UpdatedAt)
    VALUES (
        'admin',
        'adminpassword',
        (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
        1,
        GETDATE(),
        GETDATE()
    );
    
    PRINT 'Admin user created successfully.';
END
GO

-- Step 4: Verify admin user
SELECT 
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    u.CreatedAt
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

PRINT '========================================';
PRINT 'Admin Login Credentials:';
PRINT 'Username: admin';
PRINT 'Password: adminpassword';
PRINT '========================================';
GO

