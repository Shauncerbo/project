using System;

namespace project.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public int MemberID { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        
        // PayMongo payment fields
        public string? PayMongoPaymentId { get; set; }
        public string? PayMongoStatus { get; set; }
        public bool? IsOnlinePayment { get; set; } = false;

        public Member? Member { get; set; }
    }
}
