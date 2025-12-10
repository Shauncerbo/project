    -- ============================================
    -- Create Capitals and Investments Tables for GymCRM_DB
    -- ============================================

    USE GymCRM_DB;
    GO

    -- ============================================
    -- CAPITALS TABLE
    -- ============================================
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Capitals')
    BEGIN
        CREATE TABLE Capitals (
            CapitalID INT PRIMARY KEY IDENTITY(1,1),
            Amount DECIMAL(18,2) NOT NULL,
            DateAdded DATETIME NOT NULL,
            Description NVARCHAR(500) NULL,
            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
        );
        
        PRINT 'Capitals table created successfully';
    END
    ELSE
    BEGIN
        PRINT 'Capitals table already exists';
    END
    GO

    -- ============================================
    -- INVESTMENTS TABLE
    -- ============================================
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Investments')
    BEGIN
        CREATE TABLE Investments (
            InvestmentID INT PRIMARY KEY IDENTITY(1,1),
            Date DATETIME NOT NULL,
            Name NVARCHAR(200) NOT NULL,
            Description NVARCHAR(500) NULL,
            Quantity INT NOT NULL DEFAULT 1,
            Price DECIMAL(18,2) NOT NULL,
            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
        );
        
        PRINT 'Investments table created successfully';
    END
    ELSE
    BEGIN
        -- Add Name column if table exists but column doesn't
        IF NOT EXISTS (
            SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'Investments' AND COLUMN_NAME = 'Name'
        )
        BEGIN
            ALTER TABLE Investments
            ADD Name NVARCHAR(200) NOT NULL DEFAULT 'Equipment';
            
            -- Migrate existing Description to Name if Name is empty
            UPDATE Investments
            SET Name = Description
            WHERE Name = 'Equipment' OR Name IS NULL;
            
            PRINT 'Name column added to Investments table';
        END
        
        -- Add Quantity column if table exists but column doesn't
        IF NOT EXISTS (
            SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'Investments' AND COLUMN_NAME = 'Quantity'
        )
        BEGIN
            ALTER TABLE Investments
            ADD Quantity INT NOT NULL DEFAULT 1;
            
            PRINT 'Quantity column added to Investments table';
        END
        
        -- Update Description to be nullable if it's currently NOT NULL
        IF EXISTS (
            SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = 'Investments' 
            AND COLUMN_NAME = 'Description' 
            AND IS_NULLABLE = 'NO'
        )
        BEGIN
            ALTER TABLE Investments
            ALTER COLUMN Description NVARCHAR(500) NULL;
            
            PRINT 'Description column updated to nullable';
        END
        
        PRINT 'Investments table already exists';
    END
    GO

