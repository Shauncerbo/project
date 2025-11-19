using Microsoft.EntityFrameworkCore;
using project.Models;
using System;

namespace project.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor for Dependency Injection (GOOD)
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // REMOVED SQLite fallback - Use SQL Server from dependency injection only
        // The connection string is configured in MauiProgram.cs
        // This prevents fallback to empty SQLite database

        // ✅ KEEP ALL YOUR DbSet PROPERTIES
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Member> Members { get; set; }
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

        // ✅ ADD OnModelCreating for relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite key for MemberTrainer
            modelBuilder.Entity<MemberTrainer>()
                .HasKey(mt => new { mt.MemberID, mt.TrainerID });

            // Configure composite key for MemberPromo  
            modelBuilder.Entity<MemberPromo>()
                .HasKey(mp => new { mp.MemberID, mp.PromotionID });
            
            // Configure User entity - ignore columns that don't exist in database
            modelBuilder.Entity<User>(entity =>
            {
                // Ignore properties that don't exist in the database table
                entity.Ignore(u => u.FirstName);
                entity.Ignore(u => u.LastName);
                entity.Ignore(u => u.Email);
                entity.Ignore(u => u.LastLogin);
            });
        }
    }
}