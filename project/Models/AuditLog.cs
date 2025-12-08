using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogID { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityID { get; set; }

        public string? EntityName { get; set; }

        [Required]
        public string PerformedBy { get; set; } = string.Empty;

        public int? PerformedByUserID { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public string Status { get; set; } = "Success";

        public string? ErrorMessage { get; set; }
    }
}


















