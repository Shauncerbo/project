// ✅ USING STATEMENTS MUST BE AT THE TOP
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class Member
    {
        public int MemberID { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleInitial { get; set; }

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Contact number must be 11 digits.")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsArchived { get; set; } = false;

        // Foreign Keys
        public int MembershipTypeID { get; set; }
        public int? TrainerID { get; set; }
        public int? TrainerScheduleID { get; set; }

        // Navigation Properties
        public virtual MembershipType? MembershipType { get; set; }
        public virtual Trainer? Trainer { get; set; }
        public virtual TrainerSchedule? TrainerSchedule { get; set; }

        // Collections
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // Computed property for full name
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}