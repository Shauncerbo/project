-- ============================================
-- Create WalkIns Table for GymCRM_DB
-- ============================================

USE GymCRM_DB;
GO

-- ============================================
-- WALKINS TABLE
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WalkIns')
BEGIN
    CREATE TABLE WalkIns (
        WalkInID INT PRIMARY KEY IDENTITY(1,1),
        FirstName NVARCHAR(100) NOT NULL,
        MiddleName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NOT NULL,
        VisitDate DATETIME NOT NULL,
        PaymentAmount DECIMAL(10,2) NOT NULL
    );
    PRINT 'WalkIns table created successfully';
END
ELSE
BEGIN
    PRINT 'WalkIns table already exists';
END
GO

