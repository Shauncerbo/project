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

                // Test database connection
                try
                {
                    if (!await _context.Database.CanConnectAsync())
                    {
                        System.Diagnostics.Debug.WriteLine("ERROR: Cannot connect to database!");
                        throw new Exception("Cannot connect to database. Please check your SQL Server connection.");
                    }
                    System.Diagnostics.Debug.WriteLine("Database connection successful");
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Database connection error: {dbEx.Message}");
                    throw new Exception($"Database connection failed: {dbEx.Message}", dbEx);
                }

                var allUsers = await _context.Users
                    .Include(u => u.Role)
                    .ToListAsync();

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

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Login failed: User '{username}' not found in database");
                    await SecureStorage.Default.SetAsync("is_authenticated", "false");
                    return false;
                }

                // Check if user is active (treat NULL as active for backward compatibility)
                if (user.IsActive == false)
                {
                    System.Diagnostics.Debug.WriteLine($"Login failed: User '{username}' is not active (IsActive = false)");
                    await SecureStorage.Default.SetAsync("is_authenticated", "false");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"User '{username}' is active (IsActive = {(user.IsActive.HasValue ? user.IsActive.Value.ToString() : "NULL (treated as true)")})");

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

                    await SecureStorage.Default.SetAsync("is_authenticated", "true");
                    await SecureStorage.Default.SetAsync("username", user.Username);
                    await SecureStorage.Default.SetAsync("user_id", user.UserID.ToString());
                    await SecureStorage.Default.SetAsync("user_role", roleToStore);

                    // Verify storage
                    var verifyAuth = await SecureStorage.Default.GetAsync("is_authenticated");
                    System.Diagnostics.Debug.WriteLine($"Verification - is_authenticated stored as: '{verifyAuth}'");

                    if (verifyAuth != "true")
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: Authentication state not saved correctly!");
                    }
                }
                catch (Exception storageEx)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR storing authentication: {storageEx.Message}");
                    throw;
                }

                System.Diagnostics.Debug.WriteLine("Authentication data stored successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await SecureStorage.Default.SetAsync("is_authenticated", "false");
                return false;
            }
        }

        public async Task<bool> IsUserAuthenticated()
        {
            try
            {
                var authStatus = await SecureStorage.Default.GetAsync("is_authenticated");
                return authStatus == "true";
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== STARTING LOGOUT PROCESS ===");

                // Method 1: Remove individual keys with verification
                var keys = new[] { "is_authenticated", "username", "user_role", "user_id" };
                foreach (var key in keys)
                {
                    await RemoveSecureStorageKey(key);
                }

                // Method 2: Clear all storage as backup
                try
                {
                    SecureStorage.Default.RemoveAll();
                    await Task.Delay(50);
                    System.Diagnostics.Debug.WriteLine("All secure storage cleared");
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
    }
}