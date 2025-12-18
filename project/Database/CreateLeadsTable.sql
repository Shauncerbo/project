-- ============================================
-- Create Leads Table for GymCRM_DB
-- ============================================

USE GymCRM_DB;
GO

-- ============================================
-- LEADS TABLE
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Leads')
BEGIN
    CREATE TABLE Leads (
        LeadID INT PRIMARY KEY IDENTITY(1,1),
        FullName NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NULL,
        ContactNumber NVARCHAR(11) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
        LeadSource NVARCHAR(100) NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        IsArchived BIT NOT NULL DEFAULT 0
    );
    PRINT 'Leads table created successfully';
END
ELSE
BEGIN
    PRINT 'Leads table already exists';
END
GO

