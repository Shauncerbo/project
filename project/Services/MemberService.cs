using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using QRCoder;
using System.IO;

namespace project.Services
{
    public class MemberService : IMemberService
    {
        private readonly AppDbContext _context;

        public MemberService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Member> AddMemberAsync(Member member)
        {
            try
            {
                if (member.JoinDate == default)
                    member.JoinDate = DateTime.Today;

                if (string.IsNullOrEmpty(member.Status))
                    member.Status = "Active";

                _context.Members.Add(member);
                await _context.SaveChangesAsync();
                return member;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding member: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Member>> GetAllMembersAsync()
        {
            try
            {
                return await _context.Members
                    .Include(m => m.MembershipType)
                    .Include(m => m.Trainer)
                    .OrderBy(m => m.MemberID)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting members: {ex.Message}");
                return new List<Member>();
            }
        }

        public async Task<Member?> GetMemberByIdAsync(int memberId)
        {
            try
            {
                return await _context.Members
                    .Include(m => m.MembershipType)
                    .Include(m => m.Trainer)
                    .FirstOrDefaultAsync(m => m.MemberID == memberId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting member: {ex.Message}");
                return null;
            }
        }

        public async Task<Member> UpdateMemberAsync(Member member)
        {
            try
            {
                _context.Members.Update(member);
                await _context.SaveChangesAsync();
                return member;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating member: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteMemberAsync(int memberId)
        {
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member != null)
                {
                    _context.Members.Remove(member);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting member: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> GenerateMemberQrCodeAsync(int memberId)
        {
            try
            {
                var qrText = $"GYM:MEMBER:{memberId}:{DateTime.Now:yyyyMMdd}";

                using (var qrGenerator = new QRCodeGenerator())
                using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    return await Task.FromResult(qrCode.GetGraphic(20));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return null;
            }
        }
    }
}
