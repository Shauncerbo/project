using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class Trainer
    {
        public int TrainerID { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;

        public string ContactNumber { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;

        // ADD [NotMapped] attribute here
        [NotMapped]
        public string FullName =>
            $"{FirstName} {(string.IsNullOrWhiteSpace(MiddleName) ? "" : MiddleName + " ")}{LastName}".Trim();

        public ICollection<MemberTrainer>? MemberTrainers { get; set; }
    }
}