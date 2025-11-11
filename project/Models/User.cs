namespace project.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Foreign Key to Role
        public int RoleID { get; set; }

        // Navigation Property (connects to Role table)
        public Role? Role { get; set; }
    }
}
