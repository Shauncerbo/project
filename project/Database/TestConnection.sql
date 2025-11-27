    -- Test Database Connection
    -- Run this to verify your database is accessible

    USE GymCRM_DB;
    GO

    PRINT 'Testing database connection...';
    PRINT 'Database: ' + DB_NAME();
    PRINT 'Server: ' + @@SERVERNAME;
    PRINT '';

    -- Test 1: Can we query Users table?
    PRINT 'Test 1: Querying Users table...';
    IF EXISTS (SELECT 1 FROM Users)
    BEGIN
        PRINT '   ✓ Users table is accessible';
        PRINT '   ✓ Found ' + CAST((SELECT COUNT(*) FROM Users) AS VARCHAR(10)) + ' user(s)';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Users table is empty or not accessible';
    END
    GO

    -- Test 2: Can we query admin user directly?
    PRINT '';
    PRINT 'Test 2: Querying admin user...';
    DECLARE @AdminCount INT;
    SELECT @AdminCount = COUNT(*) FROM Users WHERE Username = 'admin';
    IF @AdminCount > 0
    BEGIN
        PRINT '   ✓ Admin user found';
        
        -- Get admin details
        SELECT 
            'Admin User Test' AS Test,
            Username,
            Password,
            RoleID,
            IsActive
        FROM Users
        WHERE Username = 'admin';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Admin user NOT found';
    END
    GO

    -- Test 3: Can we join with Roles?
    PRINT '';
    PRINT 'Test 3: Testing JOIN with Roles table...';
    IF EXISTS (
        SELECT 1 
        FROM Users u
        INNER JOIN Roles r ON u.RoleID = r.RoleID
        WHERE u.Username = 'admin'
    )
    BEGIN
        PRINT '   ✓ JOIN with Roles works';
        
        SELECT 
            'JOIN Test' AS Test,
            u.Username,
            r.RoleName,
            u.IsActive
        FROM Users u
        INNER JOIN Roles r ON u.RoleID = r.RoleID
        WHERE u.Username = 'admin';
    END
    ELSE
    BEGIN
        PRINT '   ✗ JOIN with Roles failed';
    END
    GO

    PRINT '';
    PRINT '========================================';
    PRINT 'Connection test complete';
    PRINT '========================================';
    GO

