using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using project.Models;

namespace project.Services
{
    public interface IAttendanceService
    {
        // Basic Attendance Operations
        Task<bool> CheckInMemberAsync(int memberId);
        Task<List<Attendance>> GetTodayAttendanceAsync();
        
        // Attendance History & Reports
        Task<List<Attendance>> GetAttendanceByDateAsync(DateTime date);

        // QR Code Attendance
        Task<bool> ProcessQrAttendanceAsync(string qrData);
        Task<string?> GenerateMemberQrCodeAsync(int memberId);  // Changed to nullable

        // Utility Methods
        Task<List<Member>> GetAllMembersAsync();
        Task<string> TestDatabaseConnection();

    }
}