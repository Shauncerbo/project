using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace project.Models
{
    public class Role
    {
        [Key]
        [Column("RoleID")]
        public int RoleID { get; set; }

        [Required]
        [Column("RoleName")]
        [MaxLength(255)]
        public string RoleName { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}