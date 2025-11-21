-- Update admin password to "adminpassword"
-- Replace 'admin' with your actual admin username if different

-- For LOCAL DATABASE (GymCRM_DB)
USE GymCRM_DB;
GO

UPDATE Users
SET Password = 'adminpassword',
    LastPasswordChange = GETDATE()
WHERE Username = 'admin';

-- Verify the update
SELECT UserID, Username, Password, LastPasswordChange
FROM Users
WHERE Username = 'admin';

-- ============================================
-- For MONSTERASP DATABASE (db32884)
-- Uncomment below if updating MonsterASP database:
-- ============================================
/*
USE db32884;
GO

UPDATE Users
SET Password = 'adminpassword',
    LastPasswordChange = GETDATE()
WHERE Username = 'admin';

-- Verify the update
SELECT UserID, Username, Password, LastPasswordChange
FROM Users
WHERE Username = 'admin';
*/

