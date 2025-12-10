-- ============================================
-- Create SystemSettings Table for GymCRM_DB
-- ============================================

USE GymCRM_DB;
GO

-- ============================================
-- SYSTEMSETTINGS TABLE
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemSettings')
BEGIN
    CREATE TABLE SystemSettings (
        SettingID INT PRIMARY KEY IDENTITY(1,1),
        SettingKey NVARCHAR(100) NOT NULL,
        SettingValue NVARCHAR(500) NULL,
        UpdatedAt DATETIME NULL
    );
    
    CREATE UNIQUE INDEX IX_SystemSettings_SettingKey ON SystemSettings(SettingKey);
    
    PRINT 'SystemSettings table created successfully';
END
ELSE
BEGIN
    PRINT 'SystemSettings table already exists';
END
GO

