using System;
using System.Collections.Generic;

namespace project.Models
{
    public class Member
    {
        public int MemberID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleInitial { get; set; } = string.Empty; 
        public string LastName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public int MembershipTypeID { get; set; }
        public required MembershipType MembershipType { get; set; }


        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();


        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();


    }
}