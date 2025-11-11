using System;

namespace project.Models
{
    public class WalkIn
    {
        public int WalkInID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}
