-- Check Admin User Status
-- Run this script to diagnose admin login issues

USE GymCRM_DB;
GO

-- Check if admin user exists
SELECT 
    UserID,
    Username,
    Password,
    RoleID,
    IsActive,
    CreatedAt,
    UpdatedAt
FROM Users
WHERE Username = 'admin';
GO

-- Check if Roles table has Admin role
SELECT RoleID, RoleName
FROM Roles
WHERE RoleName = 'Admin';
GO

-- Check if admin user has Admin role assigned
SELECT 
    u.UserID,
    u.Username,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    u.Password
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

-- If admin user doesn't exist or has wrong password, run this:
-- UPDATE Users
-- SET Password = 'adminpassword',
--     IsActive = 1,
--     RoleID = (SELECT RoleID FROM Roles WHERE RoleName = 'Admin')
-- WHERE Username = 'admin';

-- If admin user doesn't exist at all, create it:
/*
INSERT INTO Users (Username, Password, RoleID, IsActive, CreatedAt, UpdatedAt)
SELECT 
    'admin',
    'adminpassword',
    (SELECT RoleID FROM Roles WHERE RoleName = 'Admin'),
    1,
    GETDATE(),
    GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin');
*/

