    -- ============================================
    -- Create MemberPromos Table for GymCRM_DB (Local)
    -- ============================================

USE GymCRM_DB;
GO

-- ============================================
-- PROMOTIONS TABLE (Create this FIRST - MemberPromos depends on it)
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

-- ============================================
-- MEMBERPROMOS TABLE (Junction table for Members and Promotions)
-- ============================================
-- Note: This requires both Members and Promotions tables to exist first
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MemberPromos')
BEGIN
    -- Check if required tables exist
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Members') 
       AND EXISTS (SELECT * FROM sys.tables WHERE name = 'Promotions')
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
        PRINT 'ERROR: Members or Promotions table does not exist. Cannot create MemberPromos.';
    END
END
ELSE
BEGIN
    PRINT 'MemberPromos table already exists';
END
GO

    PRINT 'MemberPromos and Promotions tables setup completed!';
    GO

