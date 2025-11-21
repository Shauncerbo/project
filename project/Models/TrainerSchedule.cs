using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class TrainerSchedule
    {
        [Key]
        public int TrainerScheduleID { get; set; }

        [ForeignKey(nameof(Trainer))]
        public int TrainerID { get; set; }

        [Range(0, 6)]
        public byte DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public virtual Trainer? Trainer { get; set; }

        [NotMapped]
        public string DisplayText =>
            $"{Enum.GetName(typeof(DayOfWeek), DayOfWeek)} {DateTime.Today.Add(StartTime).ToString("hh\\:mm tt")} - {DateTime.Today.Add(EndTime).ToString("hh\\:mm tt")}";
    }
}

