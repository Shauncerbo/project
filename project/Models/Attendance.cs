using System;
using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        public DateTime CheckinTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        // Navigation property
        public virtual Member? Member { get; set; }
    }
}