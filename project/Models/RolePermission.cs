using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleID { get; set; }

        [Required]
        [MaxLength(255)]
        public string FeatureName { get; set; } = string.Empty;

        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        // Navigation property
        public virtual Role? Role { get; set; }
    }
}

