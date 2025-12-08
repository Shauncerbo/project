using project.Models;

namespace project.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendExpirationReminderAsync(Member member, int daysUntilExpiration);
        Task<bool> TestEmailConfigurationAsync();
    }
}

