    -- Add Description column to MembershipTypes table
    -- Run this script in your database

    -- For LOCAL DATABASE (GymCRM_DB)
    USE GymCRM_DB;
    GO

    IF NOT EXISTS (
        SELECT * 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'MembershipTypes' 
        AND COLUMN_NAME = 'Description'
    )
    BEGIN
        ALTER TABLE MembershipTypes
        ADD Description NVARCHAR(MAX) NULL;
        
        PRINT 'Description column added successfully to MembershipTypes table.';
    END
    ELSE
    BEGIN
        PRINT 'Description column already exists in MembershipTypes table.';
    END
    GO

    -- ============================================
    -- For MONSTERASP DATABASE (db32884)
    -- Uncomment below if updating MonsterASP database:
    -- ============================================
    /*
    USE db32884;
    GO

    IF NOT EXISTS (
        SELECT * 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'MembershipTypes' 
        AND COLUMN_NAME = 'Description'
    )
    BEGIN
        ALTER TABLE MembershipTypes
        ADD Description NVARCHAR(MAX) NULL;
        
        PRINT 'Description column added successfully to MembershipTypes table.';
    END
    ELSE
    BEGIN
        PRINT 'Description column already exists in MembershipTypes table.';
    END
    GO
    */

    PRINT 'Description column migration completed successfully!';
    GO

