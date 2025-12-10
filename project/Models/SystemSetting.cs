using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingID { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string SettingValue { get; set; } = string.Empty;
        
        public DateTime? UpdatedAt { get; set; }
    }
}

