using System.Collections.Generic;

namespace project.Models
{
    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Navigation Property (list of users who have this role)
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
