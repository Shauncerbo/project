using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class User
    {
        [Key]
        [Column("UserID")]
        public int UserID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public int RoleID { get; set; }

        public bool? IsActive { get; set; } = true; // Nullable to match database (NULL = active for backward compatibility)
        public DateTime? LastLogin { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public DateTime? CreatedAt { get; set; } // Nullable to match database
        public DateTime? UpdatedAt { get; set; } // Nullable to match database

        public int? CreatedBy { get; set; }

        // TrainerID exists in database - map it but don't use it
        [Column("TrainerID")]
        public int? TrainerID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }
    }
}