-- Create Liabilities Table for PowerHouse Gym
-- Run this script in SQL Server Management Studio

USE [PowerHouseGym];
GO

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Liabilities')
BEGIN
    CREATE TABLE [dbo].[Liabilities] (
        [LiabilityID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Date] DATETIME NOT NULL DEFAULT GETDATE(),
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [LiabilityType] NVARCHAR(50) NOT NULL DEFAULT 'Other',
        [DueDate] DATETIME NULL,
        [IsPaid] BIT NOT NULL DEFAULT 0,
        [PaidDate] DATETIME NULL,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL
    );

    PRINT 'Liabilities table created successfully.';
END
ELSE
BEGIN
    PRINT 'Liabilities table already exists.';
END
GO

-- Create index for faster queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Liabilities_IsPaid')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Liabilities_IsPaid] 
    ON [dbo].[Liabilities] ([IsPaid], [IsArchived])
    INCLUDE ([Amount]);

    PRINT 'Index created successfully.';
END
GO

-- Verify table creation
SELECT 
    c.COLUMN_NAME, 
    c.DATA_TYPE, 
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'Liabilities'
ORDER BY c.ORDINAL_POSITION;
GO
