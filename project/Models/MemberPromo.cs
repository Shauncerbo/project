using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class MemberPromo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        public int PromotionID { get; set; }  // ← ADD THIS

        public DateTime? DateUsed { get; set; }

        // Navigation properties
        public virtual Member? Member { get; set; }
        public virtual Promotion? Promotion { get; set; }
    }
}