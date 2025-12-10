using System;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Capital
    {
        [Key]
        public int CapitalID { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        public DateTime DateAdded { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

