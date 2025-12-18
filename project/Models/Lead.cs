using System;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Lead
    {
        [Key]
        public int LeadID { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(11)]
        public string? ContactNumber { get; set; }

        public string Status { get; set; } = "New";

        public string? LeadSource { get; set; }  // Facebook, Instagram, Referral, Walk-in, Promotion, Google, Website, Other

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsArchived { get; set; } = false;
    }
}

