using Microsoft.EntityFrameworkCore;
using project.Models;

namespace project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Member> Members { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<MemberTrainer> MemberTrainers { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<MembershipType> MembershipTypes { get; set; }
        public DbSet<WalkIn> WalkIns { get; set; }
        public DbSet<MemberPromo> MemberPromos { get; set; }

    }
}
