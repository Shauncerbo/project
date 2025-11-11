using System;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Promotion
    {
        [Key]                                   // ✅ tells EF this is the PK
        public int PromoID { get; set; }

        public string PromoName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
