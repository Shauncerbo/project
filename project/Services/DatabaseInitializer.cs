using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Models;

namespace project.Services
{
    public class DatabaseInitializer
    {
        private readonly AppDbContext _context;

        public DatabaseInitializer(AppDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Check if database exists, if not create it
                if (!await _context.Database.CanConnectAsync())
                {
                    // Try to apply migrations first
                    try
                    {
                        await _context.Database.MigrateAsync();
                    }
                    catch
                    {
                        // If migrations don't exist, create database using EnsureCreated
                        // This is a fallback for initial setup
                        await _context.Database.EnsureCreatedAsync();
                    }
                }
                else
                {
                    // Database exists, try to apply migrations
                    try
                    {
                        await _context.Database.MigrateAsync();
                    }
                    catch
                    {
                        // Migrations might not exist yet, that's okay
                        System.Diagnostics.Debug.WriteLine("Migrations not found, using existing database structure");
                    }
                }

                // Seed initial data
                await SeedDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                // Don't throw - allow app to continue even if DB init fails
            }
        }

        private async Task SeedDataAsync()
        {
            // Seed Roles
            if (!await _context.Roles.AnyAsync())
            {
                _context.Roles.AddRange(
                    new Role { RoleID = 1, RoleName = "Admin" },
                    new Role { RoleID = 2, RoleName = "Staff" },
                    new Role { RoleID = 3, RoleName = "Trainer" }
                );
                await _context.SaveChangesAsync();
            }

            // Seed Admin User
            if (!await _context.Users.AnyAsync(u => u.Username == "admin"))
            {
                _context.Users.Add(new User
                {
                    Username = "admin",
                    Password = "adminpassword", // Change this after first login
                    RoleID = 1, // Admin role
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    LastPasswordChange = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // Seed Membership Types
            if (!await _context.MembershipTypes.AnyAsync())
            {
                _context.MembershipTypes.AddRange(
                    new MembershipType
                    {
                        TypeName = "Basic",
                        Price = 1000.00m,
                        DurationInDays = 30,
                        IsArchived = false
                    },
                    new MembershipType
                    {
                        TypeName = "Premium",
                        Price = 2000.00m,
                        DurationInDays = 30,
                        IsArchived = false
                    },
                    new MembershipType
                    {
                        TypeName = "Gold",
                        Price = 3000.00m,
                        DurationInDays = 30,
                        IsArchived = false
                    }
                );
                await _context.SaveChangesAsync();
            }
        }
    }
}

