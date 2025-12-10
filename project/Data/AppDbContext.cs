using Microsoft.EntityFrameworkCore;
using project.Models;
using System;

namespace project.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor for Dependency Injection (must use DbContextOptions<AppDbContext>)
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
        public DbSet<TrainerSchedule> TrainerSchedules { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Capital> Capitals { get; set; }
        public DbSet<Investment> Investments { get; set; }
        public DbSet<Liability> Liabilities { get; set; }

        // ✅ ADD OnModelCreating for relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite key for MemberTrainer
            modelBuilder.Entity<MemberTrainer>()
                .HasKey(mt => new { mt.MemberID, mt.TrainerID });

            // Configure composite key for MemberPromo  
            modelBuilder.Entity<MemberPromo>(entity =>
            {
                entity.HasKey(mp => new { mp.MemberID, mp.PromotionID });
                // Configure Id as database-generated (IDENTITY column)
                // This tells EF not to insert values for Id - the database will auto-generate it
                entity.Property(mp => mp.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("Id");
            });
            
            // Configure RolePermission - map columns to match actual database schema
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.Property(rp => rp.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(rp => rp.RoleID)
                    .HasColumnName("RoleID");
                
                entity.Property(rp => rp.FeatureName)
                    .HasColumnName("FeatureName")
                    .HasMaxLength(255);
                
                entity.Property(rp => rp.CanView)
                    .HasColumnName("CanView");
                
                entity.Property(rp => rp.CanAdd)
                    .HasColumnName("CanAdd");
                
                entity.Property(rp => rp.CanEdit)
                    .HasColumnName("CanEdit");
                
                entity.Property(rp => rp.CanDelete)
                    .HasColumnName("CanDelete");
            });
            
            // Configure User entity - ignore columns that don't exist in database
            modelBuilder.Entity<User>(entity =>
            {
                // Ignore properties that don't exist in the database table
                entity.Ignore(u => u.FirstName);
                entity.Ignore(u => u.LastName);
                entity.Ignore(u => u.Email);
                entity.Ignore(u => u.LastLogin);
                
                // TrainerID exists in database - map it but don't create relationship
                // Just map the column, no foreign key relationship
                entity.Property(u => u.TrainerID).IsRequired(false);
                
                // Configure IsActive as nullable to match database
                entity.Property(u => u.IsActive).IsRequired(false);
                
                // Configure DateTime columns - make them nullable if database allows NULL
                entity.Property(u => u.CreatedAt).IsRequired(false);
                entity.Property(u => u.UpdatedAt).IsRequired(false);
                entity.Property(u => u.LastPasswordChange).IsRequired(false);
            });
            
            // Configure WalkIn entity - map PayMongo columns
            modelBuilder.Entity<WalkIn>(entity =>
            {
                // Map PayMongo payment fields
                entity.Property(w => w.PayMongoPaymentId)
                    .HasColumnName("PayMongoPaymentId")
                    .HasMaxLength(255)
                    .IsRequired(false);
                
                entity.Property(w => w.PayMongoStatus)
                    .HasColumnName("PayMongoStatus")
                    .HasMaxLength(50)
                    .IsRequired(false);
                
                entity.Property(w => w.IsOnlinePayment)
                    .HasColumnName("IsOnlinePayment")
                    .IsRequired(false)
                    .HasDefaultValue(false);
            });
        }
    }
}