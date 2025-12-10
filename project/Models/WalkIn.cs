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
        public string PaymentMethod { get; set; } = "Cash";
        public int? TrainerID { get; set; }
        public int? TrainerScheduleID { get; set; }
        public virtual Trainer? Trainer { get; set; }
        public virtual TrainerSchedule? TrainerSchedule { get; set; }
        public bool IsArchived { get; set; } = false;

        // Lead Source - tracks how walk-in customer discovered the gym
        public string? LeadSource { get; set; }  // Facebook, Instagram, Referral, Walk-in, Promotion, Google, Website, Other
        
        // PayMongo payment fields
        public string? PayMongoPaymentId { get; set; }
        public string? PayMongoStatus { get; set; }
        public bool? IsOnlinePayment { get; set; } = false;
    }
}
