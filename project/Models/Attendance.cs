using System;

namespace project.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int MemberID { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        public Member? Member { get; set; }
    }
}
