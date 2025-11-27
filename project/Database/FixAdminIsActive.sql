-- Fix Admin IsActive = NULL Issue
-- This will set IsActive to 1 (active) for the admin user

USE GymCRM_DB;
GO

PRINT 'Fixing admin user IsActive status...';
GO

-- Update IsActive to 1 for admin user
UPDATE Users
SET 
    IsActive = 1,
    UpdatedAt = GETDATE()
WHERE Username = 'admin';
GO

-- Verify the fix
SELECT 
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive,
    CASE 
        WHEN u.IsActive = 1 THEN 'ACTIVE ✓'
        ELSE 'INACTIVE ✗'
    END AS Status
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
WHERE u.Username = 'admin';
GO

PRINT '========================================';
PRINT 'Admin user IsActive has been set to 1';
PRINT 'You should now be able to login with:';
PRINT '  Username: admin';
PRINT '  Password: adminpassword';
PRINT '========================================';
GO

