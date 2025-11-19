-- Add TrainerID column to WalkIns table (optional trainer assignment)
-- Run this script in the GymCRM_DB database

USE GymCRM_DB;
GO

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'WalkIns'
      AND COLUMN_NAME = 'TrainerID'
)
BEGIN
    ALTER TABLE WalkIns
    ADD TrainerID INT NULL;

    IF EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_NAME = 'Trainers'
    )
    BEGIN
        ALTER TABLE WalkIns
        ADD CONSTRAINT FK_WalkIns_Trainers
        FOREIGN KEY (TrainerID) REFERENCES Trainers(TrainerID);
    END

    PRINT 'TrainerID column added successfully to WalkIns table.';
END
ELSE
BEGIN
    PRINT 'TrainerID column already exists in WalkIns table.';
END
GO

