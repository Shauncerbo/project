-- =============================================
-- Check Constraints and Indexes on TrainerSchedules Table
-- This will help identify if there are any database-level restrictions
-- =============================================
USE GymCRM_DB;
GO

-- 1. Check for unique constraints
PRINT '=== Unique Constraints ===';
SELECT 
    tc.CONSTRAINT_NAME,
    tc.TABLE_NAME,
    STRING_AGG(kcu.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY kcu.ORDINAL_POSITION) AS Columns
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.TABLE_NAME = 'TrainerSchedules'
    AND tc.CONSTRAINT_TYPE = 'UNIQUE'
GROUP BY tc.CONSTRAINT_NAME, tc.TABLE_NAME;
GO

-- 2. Check for check constraints
PRINT '';
PRINT '=== Check Constraints ===';
SELECT 
    CONSTRAINT_NAME,
    CHECK_CLAUSE
FROM INFORMATION_SCHEMA.CHECK_CONSTRAINTS
WHERE CONSTRAINT_NAME IN (
    SELECT CONSTRAINT_NAME 
    FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE 
    WHERE TABLE_NAME = 'TrainerSchedules'
);
GO

-- 3. Check for indexes
PRINT '';
PRINT '=== Indexes on TrainerSchedules ===';
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS IndexColumns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('TrainerSchedules')
GROUP BY i.name, i.type_desc, i.is_unique, i.is_primary_key
ORDER BY i.is_primary_key DESC, i.is_unique DESC;
GO

-- 4. Check foreign key constraints
PRINT '';
PRINT '=== Foreign Key Constraints ===';
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fc ON fk.object_id = fc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'TrainerSchedules'
    OR OBJECT_NAME(fk.referenced_object_id) = 'TrainerSchedules';
GO

PRINT '';
PRINT '=== Constraints Check Complete ===';
PRINT 'If you see any UNIQUE constraints on (TrainerID, DayOfWeek) or similar,';
PRINT 'that would prevent multiple schedules on the same day.';
GO





