-- ============================================
-- Create Database and MemberPromos Table
-- ============================================
-- This script creates the database first, then the tables
-- ============================================

-- Create the database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'GymCRM_DB')
BEGIN
    CREATE DATABASE GymCRM_DB;
    PRINT 'Database GymCRM_DB created successfully';
END
ELSE
BEGIN
    PRINT 'Database GymCRM_DB already exists';
END
GO

-- Switch to the database
USE GymCRM_DB;
GO

-- ============================================
-- MEMBERPROMOS TABLE (Junction table for Members and Promotions)
-- ============================================
-- Note: This assumes Members table already exists
-- If Members table doesn't exist, run your app first to create it
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MemberPromos')
BEGIN
    -- Check if Members table exists first
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Members')
    BEGIN
        CREATE TABLE MemberPromos (
            Id INT PRIMARY KEY IDENTITY(1,1),
            MemberID INT NOT NULL,
            PromotionID INT NOT NULL,
            DateUsed DATETIME NULL,
            CONSTRAINT FK_MemberPromos_Members FOREIGN KEY (MemberID) REFERENCES Members(MemberID),
            CONSTRAINT FK_MemberPromos_Promotions FOREIGN KEY (PromotionID) REFERENCES Promotions(PromoID),
            CONSTRAINT UQ_MemberPromo UNIQUE (MemberID, PromotionID)
        );
        PRINT 'MemberPromos table created successfully';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Members table does not exist. Please run your app first to create the base tables.';
    END
END
ELSE
BEGIN
    PRINT 'MemberPromos table already exists';
END
GO

-- ============================================
-- PROMOTIONS TABLE (if it doesn't exist)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Promotions')
BEGIN
    CREATE TABLE Promotions (
        PromoID INT PRIMARY KEY IDENTITY(1,1),
        PromoName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        DiscountRate DECIMAL(5,2) NOT NULL,
        StartDate DATETIME NOT NULL,
        EndDate DATETIME NOT NULL
    );
    PRINT 'Promotions table created successfully';
END
ELSE
BEGIN
    PRINT 'Promotions table already exists';
END
GO

PRINT 'MemberPromos and Promotions tables setup completed!';
GO

