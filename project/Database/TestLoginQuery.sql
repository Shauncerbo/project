-- Test the exact query that the application uses
-- This simulates what AuthService.LoginAsync does

USE GymCRM_DB;
GO

PRINT 'Testing exact login query...';
PRINT '';

-- Simulate the application's query
-- Step 1: Get all users with roles (like AuthService does)
SELECT 
    'Step 1: All Users with Roles' AS TestStep,
    u.UserID,
    u.Username,
    u.Password,
    u.RoleID,
    r.RoleName,
    u.IsActive
FROM Users u
LEFT JOIN Roles r ON u.RoleID = r.RoleID
ORDER BY u.UserID;
GO

-- Step 2: Find admin user (case-insensitive)
PRINT '';
PRINT 'Step 2: Finding admin user (case-insensitive)...';
DECLARE @SearchUsername NVARCHAR(255) = 'admin';
DECLARE @FoundUsername NVARCHAR(255);

SELECT @FoundUsername = Username
FROM Users
WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@SearchUsername)));

IF @FoundUsername IS NOT NULL
BEGIN
    PRINT '   ✓ Found user: ' + @FoundUsername;
    
    -- Step 3: Check password
    DECLARE @StoredPassword NVARCHAR(255);
    DECLARE @InputPassword NVARCHAR(255) = 'adminpassword';
    
    SELECT @StoredPassword = Password
    FROM Users
    WHERE Username = @FoundUsername;
    
    PRINT '';
    PRINT 'Step 3: Password comparison...';
    PRINT '   Stored password: ' + ISNULL(@StoredPassword, 'NULL');
    PRINT '   Input password: ' + @InputPassword;
    PRINT '   Stored length: ' + CAST(LEN(@StoredPassword) AS VARCHAR(10));
    PRINT '   Input length: ' + CAST(LEN(@InputPassword) AS VARCHAR(10));
    
    IF @StoredPassword = @InputPassword
    BEGIN
        PRINT '   ✓ Passwords MATCH';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Passwords DO NOT MATCH';
        PRINT '   → This is the problem!';
        
        -- Check for hidden characters
        DECLARE @StoredBytes VARBINARY(255);
        DECLARE @InputBytes VARBINARY(255);
        SET @StoredBytes = CONVERT(VARBINARY(255), @StoredPassword);
        SET @InputBytes = CONVERT(VARBINARY(255), @InputPassword);
        
        PRINT '';
        PRINT '   Byte comparison:';
        PRINT '   Stored bytes: ' + CONVERT(VARCHAR(255), @StoredBytes, 2);
        PRINT '   Input bytes: ' + CONVERT(VARCHAR(255), @InputBytes, 2);
    END
    
    -- Step 4: Check IsActive
    DECLARE @IsActive BIT;
    SELECT @IsActive = IsActive FROM Users WHERE Username = @FoundUsername;
    
    PRINT '';
    PRINT 'Step 4: IsActive check...';
    IF @IsActive IS NULL
    BEGIN
        PRINT '   ⚠ IsActive is NULL (should be treated as active)';
    END
    ELSE IF @IsActive = 1
    BEGIN
        PRINT '   ✓ IsActive is 1 (ACTIVE)';
    END
    ELSE
    BEGIN
        PRINT '   ✗ IsActive is 0 (INACTIVE) - This will block login!';
    END
    
    -- Step 5: Check Role
    DECLARE @RoleName NVARCHAR(255);
    SELECT @RoleName = r.RoleName
    FROM Users u
    LEFT JOIN Roles r ON u.RoleID = r.RoleID
    WHERE u.Username = @FoundUsername;
    
    PRINT '';
    PRINT 'Step 5: Role check...';
    IF @RoleName = 'Admin'
    BEGIN
        PRINT '   ✓ Role is Admin';
    END
    ELSE
    BEGIN
        PRINT '   ✗ Role is NOT Admin (Current: ' + ISNULL(@RoleName, 'NULL') + ')';
    END
    
    -- Final verdict
    PRINT '';
    PRINT '========================================';
    IF @StoredPassword = @InputPassword 
       AND (@IsActive IS NULL OR @IsActive = 1)
       AND @RoleName = 'Admin'
    BEGIN
        PRINT '✓✓✓ ALL CHECKS PASSED - LOGIN SHOULD WORK ✓✓✓';
    END
    ELSE
    BEGIN
        PRINT '✗✗✗ ISSUES FOUND - LOGIN WILL FAIL ✗✗✗';
        PRINT '';
        IF @StoredPassword != @InputPassword
            PRINT '   - Password mismatch';
        IF @IsActive = 0
            PRINT '   - User is inactive';
        IF @RoleName != 'Admin'
            PRINT '   - Wrong role assigned';
    END
    PRINT '========================================';
END
ELSE
BEGIN
    PRINT '   ✗ User NOT FOUND';
END
GO

