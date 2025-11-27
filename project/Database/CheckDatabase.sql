-- ============================================
-- Check what databases exist on your SQL Server
-- ============================================
-- Run this to see all databases on your current connection

-- Show all databases
SELECT name, database_id, create_date 
FROM sys.databases 
ORDER BY name;

-- Check if GymCRM_DB exists
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'GymCRM_DB')
BEGIN
    PRINT '✅ GymCRM_DB database EXISTS';
    
    -- Switch to it and check tables
    USE GymCRM_DB;
    GO
    
    -- Show all tables
    SELECT TABLE_NAME 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_TYPE = 'BASE TABLE'
    ORDER BY TABLE_NAME;
    
    -- Check if Members table exists
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Members')
    BEGIN
        PRINT '✅ Members table EXISTS';
    END
    ELSE
    BEGIN
        PRINT '❌ Members table DOES NOT EXIST';
    END
    
    -- Check if Promotions table exists
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Promotions')
    BEGIN
        PRINT '✅ Promotions table EXISTS';
    END
    ELSE
    BEGIN
        PRINT '❌ Promotions table DOES NOT EXIST';
    END
END
ELSE
BEGIN
    PRINT '❌ GymCRM_DB database DOES NOT EXIST';
    PRINT 'You may need to:';
    PRINT '1. Run your app first to create it, OR';
    PRINT '2. Check if you''re connected to the correct server: LAPTOP-3VCGD3TV\SQLEXPRESS';
END
GO

