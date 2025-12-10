using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using project.Data;
using project.Models;
using Microsoft.EntityFrameworkCore;

namespace project.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        Task<bool> IsUserAuthenticated();
        Task LogoutAsync();
        Task<string> GetCurrentUserRole();
        Task<string> GetCurrentUsername();
        Task<int?> GetCurrentUserId();
        Task<int?> GetCurrentTrainerId();
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    System.Diagnostics.Debug.WriteLine("Login failed: Username or password is empty");
                    await SecureStorage.Default.SetAsync("is_authenticated", "false");
                    return false;
                }

                // Trim whitespace from inputs
                username = username.Trim();
                password = password.Trim();

                System.Diagnostics.Debug.WriteLine($"Attempting login for username: '{username}'");
                System.Diagnostics.Debug.WriteLine($"Password length: {password.Length}");

                // Query database for user with role (case-insensitive username comparison)
                // Load all users first, then filter in memory to ensure case-insensitive comparison works correctly
                System.Diagnostics.Debug.WriteLine("Querying database for users...");

                // Skip CanConnectAsync check - it can fail even when database is accessible
                // Instead, we'll test the connection by trying to query
                System.Diagnostics.Debug.WriteLine("Skipping CanConnectAsync check - will test connection by querying...");

                // Query users - use simple query first, then load roles separately
                List<User> allUsers;
                try
                    {
                    System.Diagnostics.Debug.WriteLine("Attempting to query Users table (simple query, no Include)...");
                    
                    // Use simple query without Include to avoid relationship issues
                    allUsers = await _context.Users
                        .AsNoTracking()
                        .ToListAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"✓ Successfully loaded {allUsers.Count} users from database");
                    
                    // Load roles separately for each user
                    System.Diagnostics.Debug.WriteLine("Loading roles for users...");
                    foreach (var u in allUsers)
                    {
                        if (u.RoleID > 0)
                        {
                            try
                            {
                                u.Role = await _context.Roles
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(r => r.RoleID == u.RoleID);
                                if (u.Role != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"  - User '{u.Username}' has role: {u.Role.RoleName}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"  - WARNING: User '{u.Username}' has RoleID {u.RoleID} but role not found!");
                                }
                            }
                            catch (Exception roleEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"  - ERROR loading role for user '{u.Username}': {roleEx.Message}");
                                u.Role = null;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  - User '{u.Username}' has no RoleID (RoleID = {u.RoleID})");
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("✓ Roles loaded for all users");
                }
                catch (Exception queryEx)
                {
                    System.Diagnostics.Debug.WriteLine($"✗✗✗ CRITICAL ERROR querying Users table ✗✗✗");
                    System.Diagnostics.Debug.WriteLine($"Error message: {queryEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Error type: {queryEx.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {queryEx.InnerException?.Message ?? "None"}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {queryEx.StackTrace}");
                    
                    if (queryEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception type: {queryEx.InnerException.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"Inner stack trace: {queryEx.InnerException.StackTrace}");
                    }
                    
                    // Re-throw with more context
                    throw new Exception($"Database query failed: {queryEx.Message}. " +
                                      $"This usually means: (1) Database connection failed, (2) Table 'Users' doesn't exist, " +
                                      $"or (3) Column mapping mismatch. Check Debug output for details.", queryEx);
                }

                System.Diagnostics.Debug.WriteLine($"Found {allUsers.Count} users in database");
#if DEBUG
                foreach (var u in allUsers)
                {
                    System.Diagnostics.Debug.WriteLine($"  - User: '{u.Username}' (ID: {u.UserID}, Active: {u.IsActive}, Role: {u.Role?.RoleName ?? "None"})");
                }
#endif

                var user = allUsers.FirstOrDefault(u =>
                    string.Equals(u.Username?.Trim(), username, StringComparison.OrdinalIgnoreCase));

                System.Diagnostics.Debug.WriteLine($"User lookup result: {(user != null ? $"Found user '{user.Username}'" : "User not found")}");
                
                // If Role is null, try to load it explicitly
                if (user != null && user.Role == null && user.RoleID > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Role is null, loading RoleID {user.RoleID} explicitly...");
                    user.Role = await _context.Roles.FindAsync(user.RoleID);
                    System.Diagnostics.Debug.WriteLine($"Role loaded: {user.Role?.RoleName ?? "Still null"}");
                }

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Login failed: User '{username}' not found in database");
                    await SecureStorage.Default.SetAsync("is_authenticated", "false");
                    return false;
                }

                // Check if user is active (treat NULL as active for backward compatibility)
                // IsActive can be NULL, true (1), or false (0)
                // Only block login if IsActive is explicitly false (0)
                if (user.IsActive.HasValue && user.IsActive.Value == false)
                {
                    System.Diagnostics.Debug.WriteLine($"Login failed: User '{username}' is not active (IsActive = false)");
                    await SecureStorage.Default.SetAsync("is_authenticated", "false");
                    return false;
                }

                // If IsActive is NULL or true, allow login
                var isActiveStatus = user.IsActive.HasValue ? user.IsActive.Value.ToString() : "NULL (treated as active)";
                System.Diagnostics.Debug.WriteLine($"User '{username}' is active (IsActive = {isActiveStatus})");

                // Check password (trim and compare - currently plain text - consider hashing in production)
                var dbPassword = user.Password?.Trim() ?? "";
                var inputPassword = password.Trim();

                System.Diagnostics.Debug.WriteLine($"=== PASSWORD COMPARISON ===");
                System.Diagnostics.Debug.WriteLine($"DB Password: '{dbPassword}' (length: {dbPassword.Length})");
                System.Diagnostics.Debug.WriteLine($"Input Password: '{inputPassword}' (length: {inputPassword.Length})");
                System.Diagnostics.Debug.WriteLine($"Are equal (Ordinal): {string.Equals(dbPassword, inputPassword, StringComparison.Ordinal)}");
                System.Diagnostics.Debug.WriteLine($"Are equal (OrdinalIgnoreCase): {string.Equals(dbPassword, inputPassword, StringComparison.OrdinalIgnoreCase)}");

                // Show byte representation for debugging
                var dbBytes = System.Text.Encoding.UTF8.GetBytes(dbPassword);
                var inputBytes = System.Text.Encoding.UTF8.GetBytes(inputPassword);
                System.Diagnostics.Debug.WriteLine($"DB Password bytes: [{string.Join(",", dbBytes)}]");
                System.Diagnostics.Debug.WriteLine($"Input Password bytes: [{string.Join(",", inputBytes)}]");

                // Check for any non-printable characters
                var dbHasNonPrintable = dbPassword.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c));
                var inputHasNonPrintable = inputPassword.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c));
                System.Diagnostics.Debug.WriteLine($"DB has non-printable chars: {dbHasNonPrintable}");
                System.Diagnostics.Debug.WriteLine($"Input has non-printable chars: {inputHasNonPrintable}");

                if (!string.Equals(dbPassword, inputPassword, StringComparison.Ordinal))
                {
                    System.Diagnostics.Debug.WriteLine($"Login failed: Password mismatch for user '{username}'");
                    System.Diagnostics.Debug.WriteLine($"Expected: '{dbPassword}'");
                    System.Diagnostics.Debug.WriteLine($"Got: '{inputPassword}'");
                    await SecureStorage.Default.SetAsync("is_authenticated", "false");
                    return false;
                }

                var roleName = user.Role?.RoleName ?? "None";
                System.Diagnostics.Debug.WriteLine($"Login successful for user: {username}, RoleID: {user.RoleID}, RoleName: '{roleName}'");

                if (string.IsNullOrEmpty(roleName) || roleName == "None")
                {
                    System.Diagnostics.Debug.WriteLine($"WARNING: User '{username}' has no role assigned (RoleID: {user.RoleID})");
                }

                // Note: LastLogin column doesn't exist in database, so we skip updating it
                // If you add the column later, uncomment this:
                // user.LastLogin = DateTime.UtcNow;
                // await _context.SaveChangesAsync();

                // Store authentication data
                System.Diagnostics.Debug.WriteLine("Storing authentication data in SecureStorage...");
                try
                {
                    var roleToStore = user.Role?.RoleName ?? "Member";
                    System.Diagnostics.Debug.WriteLine($"Storing role as: '{roleToStore}'");

                    // Store all authentication data
                    await SecureStorage.Default.SetAsync("is_authenticated", "true");
                    await SecureStorage.Default.SetAsync("username", user.Username);
                    await SecureStorage.Default.SetAsync("user_id", user.UserID.ToString());
                    await SecureStorage.Default.SetAsync("user_role", roleToStore);

                    // If this user is linked to a trainer record, store TrainerID as well
                    if (user.TrainerID.HasValue)
                    {
                        await SecureStorage.Default.SetAsync("trainer_id", user.TrainerID.Value.ToString());
                        System.Diagnostics.Debug.WriteLine($"Storing trainer_id = {user.TrainerID.Value}");
                    }
                    else
                    {
                        // Ensure no stale trainer_id is left from previous logins
                        SecureStorage.Default.Remove("trainer_id");
                        System.Diagnostics.Debug.WriteLine("No TrainerID for user; cleared any existing trainer_id");
                    }

                    // Small delay to ensure storage is committed
                    await Task.Delay(100);

                    // Verify storage multiple times
                    var verifyAuth = await SecureStorage.Default.GetAsync("is_authenticated");
                    var verifyUsername = await SecureStorage.Default.GetAsync("username");
                    var verifyRole = await SecureStorage.Default.GetAsync("user_role");
                    
                    System.Diagnostics.Debug.WriteLine($"Verification - is_authenticated: '{verifyAuth}'");
                    System.Diagnostics.Debug.WriteLine($"Verification - username: '{verifyUsername}'");
                    System.Diagnostics.Debug.WriteLine($"Verification - user_role: '{verifyRole}'");

                    if (verifyAuth != "true")
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: Authentication state not saved correctly! Retrying...");
                        // Retry once
                        await SecureStorage.Default.SetAsync("is_authenticated", "true");
                        await Task.Delay(100);
                        verifyAuth = await SecureStorage.Default.GetAsync("is_authenticated");
                        System.Diagnostics.Debug.WriteLine($"Retry verification - is_authenticated: '{verifyAuth}'");
                    }

                    if (verifyAuth != "true")
                    {
                        System.Diagnostics.Debug.WriteLine("ERROR: Authentication state still not saved after retry!");
                        throw new Exception("Failed to save authentication state to SecureStorage");
                    }

                    System.Diagnostics.Debug.WriteLine("✓ Authentication data stored and verified successfully");
                }
                catch (Exception storageEx)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR storing authentication: {storageEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {storageEx.StackTrace}");
                    throw;
                }

                System.Diagnostics.Debug.WriteLine("Authentication data stored successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== LOGIN ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Full exception: {ex}");
                await SecureStorage.Default.SetAsync("is_authenticated", "false");
                return false;
            }
        }

        public async Task<bool> IsUserAuthenticated()
        {
            try
            {
                var authStatus = await SecureStorage.Default.GetAsync("is_authenticated");
                var result = authStatus == "true";
                System.Diagnostics.Debug.WriteLine($"IsUserAuthenticated check: authStatus='{authStatus}', result={result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsUserAuthenticated error: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== STARTING LOGOUT PROCESS ===");

                // Save PayMongo API key and Gmail credentials before clearing storage
                string? payMongoKey = null;
                string? gmailAddress = null;
                string? gmailPassword = null;
                
                try
                {
                    payMongoKey = await SecureStorage.Default.GetAsync("paymongo_secret_key");
                    System.Diagnostics.Debug.WriteLine($"PayMongo key found: {!string.IsNullOrEmpty(payMongoKey)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading PayMongo key: {ex.Message}");
                }
                
                try
                {
                    gmailAddress = await SecureStorage.Default.GetAsync("gmail_address");
                    gmailPassword = await SecureStorage.Default.GetAsync("gmail_app_password");
                    System.Diagnostics.Debug.WriteLine($"Gmail config found: {!string.IsNullOrEmpty(gmailAddress) && !string.IsNullOrEmpty(gmailPassword)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading Gmail config: {ex.Message}");
                }

                // Method 1: Remove individual keys with verification
                var keys = new[] { "is_authenticated", "username", "user_role", "user_id", "trainer_id" };
                foreach (var key in keys)
                {
                    await RemoveSecureStorageKey(key);
                }

                // Method 2: Clear all storage as backup (but preserve PayMongo key)
                try
                {
                    SecureStorage.Default.RemoveAll();
                    await Task.Delay(50);
                    System.Diagnostics.Debug.WriteLine("All secure storage cleared");
                    
                    // Restore PayMongo API key if it existed
                    if (!string.IsNullOrEmpty(payMongoKey))
                    {
                        await SecureStorage.Default.SetAsync("paymongo_secret_key", payMongoKey);
                        System.Diagnostics.Debug.WriteLine("PayMongo API key restored after logout");
                    }
                    
                    // Restore Gmail credentials if they existed
                    if (!string.IsNullOrEmpty(gmailAddress) && !string.IsNullOrEmpty(gmailPassword))
                    {
                        await SecureStorage.Default.SetAsync("gmail_address", gmailAddress);
                        await SecureStorage.Default.SetAsync("gmail_app_password", gmailPassword);
                        System.Diagnostics.Debug.WriteLine("Gmail credentials restored after logout");
                    }
                }
                catch (Exception clearEx)
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveAll error: {clearEx.Message}");
                }

                // Final verification
                await Task.Delay(100);
                var finalAuthCheck = await SecureStorage.Default.GetAsync("is_authenticated");
                var finalUserCheck = await SecureStorage.Default.GetAsync("username");

                System.Diagnostics.Debug.WriteLine($"=== LOGOUT COMPLETE ===");
                System.Diagnostics.Debug.WriteLine($"Final is_authenticated: '{finalAuthCheck}'");
                System.Diagnostics.Debug.WriteLine($"Final username: '{finalUserCheck}'");
                System.Diagnostics.Debug.WriteLine($"Logout successful: {(finalAuthCheck == null ? "YES" : "NO")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL LOGOUT ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Helper method to safely remove secure storage keys
        private async Task RemoveSecureStorageKey(string key)
        {
            try
            {
                // Remove the key
                SecureStorage.Default.Remove(key);

                // Small delay to ensure the removal is processed
                await Task.Delay(20);

                // Verify it's gone
                var verify = await SecureStorage.Default.GetAsync(key);
                if (verify == null)
                {
                    System.Diagnostics.Debug.WriteLine($"✓ Successfully removed: {key}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Key {key} still exists after removal, value: '{verify}'");
                    // Try one more time
                    SecureStorage.Default.Remove(key);
                    await Task.Delay(20);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing {key}: {ex.Message}");
            }
        }

        public async Task<string> GetCurrentUserRole()
        {
            try
            {
                var role = await SecureStorage.Default.GetAsync("user_role");
                return role ?? "Member";
            }
            catch
            {
                return "Member";
            }
        }

        public async Task<string> GetCurrentUsername()
        {
            try
            {
                var username = await SecureStorage.Default.GetAsync("username");
                return username ?? "Guest";
            }
            catch
            {
                return "Guest";
            }
        }

        public async Task<int?> GetCurrentUserId()
        {
            try
            {
                var idStr = await SecureStorage.Default.GetAsync("user_id");
                if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out var id))
                {
                    return id;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<int?> GetCurrentTrainerId()
        {
            try
            {
                var trainerStr = await SecureStorage.Default.GetAsync("trainer_id");
                if (!string.IsNullOrEmpty(trainerStr) && int.TryParse(trainerStr, out var trainerId))
                {
                    return trainerId;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}