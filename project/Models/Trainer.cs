using System.Collections.Generic;

namespace project.Models
{
    public class Trainer
    {
        public int TrainerID { get; set; }     // Database Primary Key

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;

        public string ContactNumber { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;

        // Computed Property (not stored in DB)
        public string FullName =>
            $"{FirstName} {(string.IsNullOrWhiteSpace(MiddleName) ? "" : MiddleName + " ")}{LastName}".Trim();

        // Relationship (optional, keep it)
        public ICollection<MemberTrainer>? MemberTrainers { get; set; }
    }
}
