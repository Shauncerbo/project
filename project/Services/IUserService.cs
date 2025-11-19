using project.Models;

namespace project.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> CreateUserAsync(User user, int createdByUserId);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        Task<bool> ToggleUserStatusAsync(int userId);
        Task<bool> ChangeUserRoleAsync(int userId, int newRoleId);
        Task<List<Role>> GetRolesAsync();
    }
}