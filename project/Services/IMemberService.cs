using System.Collections.Generic;
using System.Threading.Tasks;
using project.Models;

namespace project.Services
{
    public interface IMemberService
    {
        Task<Member> AddMemberAsync(Member member);
        Task<List<Member>> GetAllMembersAsync();
        Task<Member?> GetMemberByIdAsync(int memberId); // Changed to nullable
        Task<Member> UpdateMemberAsync(Member member);
        Task<bool> DeleteMemberAsync(int memberId);
        Task<byte[]?> GenerateMemberQrCodeAsync(int memberId); // Changed to nullable
    }
}