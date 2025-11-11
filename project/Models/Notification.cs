using System;

namespace project.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }
        public int MemberID { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime DateSent { get; set; }

        public Member? Member { get; set; }
    }
}
