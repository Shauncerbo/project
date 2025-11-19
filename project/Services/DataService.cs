using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Models;

namespace project.Services
{
    public class DataService
    {
        private readonly AppDbContext _context;

        // Let DI inject the DbContext
        public DataService(AppDbContext context)
        {
            _context = context;
        }

        // Your methods here, for example:
        public async Task<List<Member>> GetMembersAsync()
        {
            return await _context.Members.ToListAsync();
        }

        public async Task AddMemberAsync(Member member)
        {
            _context.Members.Add(member);
            await _context.SaveChangesAsync();
        }

        // Add other methods for Trainers, Payments, etc.
    }
}