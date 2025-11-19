using System;

namespace project.Models
{
    public class WalkIn
    {
        public int WalkInID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public int? TrainerID { get; set; }
        public virtual Trainer? Trainer { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}
