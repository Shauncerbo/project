-- =============================================
-- TrainerSchedules table for GymCRM_DB
-- =============================================
USE GymCRM_DB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrainerSchedules')
BEGIN
    CREATE TABLE TrainerSchedules (
        TrainerScheduleID INT IDENTITY(1,1) PRIMARY KEY,
        TrainerID INT NOT NULL FOREIGN KEY REFERENCES Trainers(TrainerID),
        DayOfWeek TINYINT NOT NULL CHECK (DayOfWeek BETWEEN 0 AND 6),
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        IsAvailable BIT NOT NULL DEFAULT 1
    );

    PRINT 'TrainerSchedules table created.';
END
ELSE
BEGIN
    PRINT 'TrainerSchedules table already exists.';
END
GO

-- Add TrainerScheduleID column to Members if missing
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Members' AND COLUMN_NAME = 'TrainerScheduleID'
)
BEGIN
    ALTER TABLE Members
    ADD TrainerScheduleID INT NULL
        CONSTRAINT FK_Members_TrainerSchedules
            FOREIGN KEY REFERENCES TrainerSchedules(TrainerScheduleID);

    PRINT 'TrainerScheduleID column added to Members table.';
END
ELSE
BEGIN
    PRINT 'TrainerScheduleID column already exists in Members table.';
END
GO

-- Add TrainerScheduleID column to WalkIns if missing
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'WalkIns' AND COLUMN_NAME = 'TrainerScheduleID'
)
BEGIN
    ALTER TABLE WalkIns
    ADD TrainerScheduleID INT NULL
        CONSTRAINT FK_WalkIns_TrainerSchedules
            FOREIGN KEY REFERENCES TrainerSchedules(TrainerScheduleID);

    PRINT 'TrainerScheduleID column added to WalkIns table.';
END
ELSE
BEGIN
    PRINT 'TrainerScheduleID column already exists in WalkIns table.';
END
GO

