using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace project.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckInMemberAsync(int memberId)
        {
            try
            {
                // Check if member exists
                var member = await _context.Members.FindAsync(memberId);
                if (member == null)
                    return false;

                if (!IsMemberEligible(member))
                    return false;

                // Check if member already verified today
                var today = DateTime.Today;
                var existingCheckIn = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.MemberID == memberId && a.CheckinTime >= today);

                if (existingCheckIn != null)
                {
                    // Already verified today
                    return true;
                }

                // Create new verification record
                var attendance = new Attendance
                {
                    MemberID = memberId,
                    CheckinTime = DateTime.Now,
                    CheckOutTime = null
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking in member: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Attendance>> GetTodayAttendanceAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                return await _context.Attendances
                    .Include(a => a.Member)
                    .Where(a => a.CheckinTime >= today && a.CheckinTime < tomorrow)
                    .OrderByDescending(a => a.CheckinTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting today's attendance: {ex.Message}");
                return new List<Attendance>();
            }
        }

        public async Task<List<Attendance>> GetAttendanceByDateAsync(DateTime date)
        {
            try
            {
                var nextDay = date.AddDays(1);

                return await _context.Attendances
                    .Include(a => a.Member)
                    .Where(a => a.CheckinTime >= date && a.CheckinTime < nextDay)
                    .OrderByDescending(a => a.CheckinTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting attendance by date: {ex.Message}");
                return new List<Attendance>();
            }
        }

        public async Task<bool> ProcessQrAttendanceAsync(string qrData)
        {
            try
            {
                if (qrData.StartsWith("GYM:MEMBER:"))
                {
                    var parts = qrData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int memberId))
                    {
                        var member = await _context.Members.FindAsync(memberId);
                        if (member == null)
                            return false;

                        if (!IsMemberEligible(member))
                            return false;

                        // Check if member already verified today
                        var today = DateTime.Today;
                        var existing = await _context.Attendances
                            .AnyAsync(a => a.MemberID == memberId && a.CheckinTime >= today);

                        if (existing)
                        {
                            return true;
                        }

                        return await CheckInMemberAsync(memberId);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing QR attendance: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GenerateMemberQrCodeAsync(int memberId)
        {
            try
            {
                var qrText = $"GYM:MEMBER:{memberId}:{DateTime.Now:yyyyMMdd}";
                return await Task.FromResult(qrText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Member>> GetAllMembersAsync()
        {
            try
            {
                return await _context.Members
                    .Where(m => m.Status == "Active")
                    .OrderBy(m => m.FirstName)
                    .ThenBy(m => m.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all members: {ex.Message}");
                return new List<Member>();
            }
        }

        public async Task<string> TestDatabaseConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return canConnect ? "Database connection successful" : "Database connection failed";
            }
            catch (Exception ex)
            {
                return $"Database connection error: {ex.Message}";
            }
        }

        private bool IsMemberEligible(Member member)
        {
            if (member.IsArchived)
                return false;

            if (!string.Equals(member.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return false;

            if (member.ExpirationDate.HasValue && member.ExpirationDate.Value.Date < DateTime.Today)
                return false;

            return true;
        }

    }
}