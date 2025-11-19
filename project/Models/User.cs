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
        [Column("FirstName", TypeName = "nvarchar(100)")]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        [Column("LastName", TypeName = "nvarchar(100)")]
        public string? LastName { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        [Column("Email", TypeName = "nvarchar(255)")]
        public string? Email { get; set; }

        public int RoleID { get; set; }

        public bool? IsActive { get; set; } = true;
        
        [Column("LastLogin", TypeName = "datetime2")]
        public DateTime? LastLogin { get; set; }
        
        [Column("LastPasswordChange", TypeName = "datetime2")]
        public DateTime? LastPasswordChange { get; set; }
        
        [Column("CreatedAt", TypeName = "datetime2")]
        public DateTime? CreatedAt { get; set; }
        
        [Column("UpdatedAt", TypeName = "datetime2")]
        public DateTime? UpdatedAt { get; set; }

        public int? CreatedBy { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }
    }
}