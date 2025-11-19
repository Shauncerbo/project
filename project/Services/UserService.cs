using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace project.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .OrderBy(u => u.UserID)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(User user, int createdByUserId)
        {
            try
            {
                // Set creation details
                // You might want to add CreatedDate and CreatedBy fields to your User model
                // user.CreatedDate = DateTime.Now;
                // user.CreatedBy = createdByUserId;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null)
                    return false;

                // Update properties
                existingUser.Username = user.Username;
                existingUser.RoleID = user.RoleID;
                // Add other properties as needed

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.Password = newPassword; // In production, hash this password!
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting password: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                // Assuming you have an IsActive property - adjust based on your User model
                // If you don't have status, you might need to add it or remove this method
                // user.IsActive = !user.IsActive;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling user status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangeUserRoleAsync(int userId, int newRoleId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                user.RoleID = newRoleId;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing user role: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            try
            {
                return await _context.Roles
                    .OrderBy(r => r.RoleID)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting roles: {ex.Message}");
                return new List<Role>();
            }
        }
    }
}