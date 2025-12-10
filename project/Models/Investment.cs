using System;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Investment
    {
        [Key]
        public int InvestmentID { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int Quantity { get; set; } = 1;
        
        [Required]
        public decimal Price { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

