using System.Net;
using System.Net.Mail;
using Microsoft.Maui.Storage;
using project.Models;

namespace project.Services
{
    public class EmailService : IEmailService
    {
        private const string GmailSmtpServer = "smtp.gmail.com";
        private const int GmailSmtpPort = 587;

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Get Gmail credentials from SecureStorage
                var gmailAddress = await SecureStorage.Default.GetAsync("gmail_address");
                var gmailAppPassword = await SecureStorage.Default.GetAsync("gmail_app_password");

                if (string.IsNullOrEmpty(gmailAddress) || string.IsNullOrEmpty(gmailAppPassword))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Gmail credentials not configured in SecureStorage");
                    return false;
                }

                using var client = new SmtpClient(GmailSmtpServer, GmailSmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(gmailAddress, gmailAppPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(gmailAddress, "Gym Management System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                System.Diagnostics.Debug.WriteLine($"✅ Email sent successfully to {toEmail}");
                return true;
            }
            catch (SmtpException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SMTP Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendExpirationReminderAsync(Member member, int daysUntilExpiration)
        {
            if (string.IsNullOrEmpty(member.Email))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Member {member.MemberID} has no email address");
                return false;
            }

            var expirationDate = member.ExpirationDate?.ToString("MM/dd/yyyy") ?? "N/A";
            var membershipType = member.MembershipType?.TypeName ?? "Membership";
            var subject = $"⚠️ Gym Membership Expiring Soon - {daysUntilExpiration} Day{(daysUntilExpiration > 1 ? "s" : "")} Remaining";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .warning-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px; }}
                        .info-box {{ background: #e7f3ff; border-left: 4px solid #2196F3; padding: 15px; margin: 20px 0; border-radius: 5px; }}
                        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; }}
                        .days-remaining {{ font-size: 32px; font-weight: bold; color: #e74c3c; margin: 10px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🏋️ Membership Expiration Reminder</h1>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{member.FirstName} {member.LastName}</strong>,</p>
                            
                            <div class='warning-box'>
                                <h3 style='margin-top: 0; color: #856404;'>⚠️ Your Membership is Expiring Soon!</h3>
                                <div class='days-remaining'>{daysUntilExpiration} Day{(daysUntilExpiration > 1 ? "s" : "")} Remaining</div>
                                <p style='margin-bottom: 0;'><strong>Expiration Date:</strong> {expirationDate}</p>
                            </div>

                            <div class='info-box'>
                                <p style='margin-top: 0;'><strong>Membership Type:</strong> {membershipType}</p>
                                <p style='margin-bottom: 0;'>Please renew your membership to continue enjoying our services and facilities.</p>
                            </div>

                            <p>We value you as a member and hope to continue serving you. If you have any questions or need assistance with renewal, please don't hesitate to contact us.</p>

                            <p>Thank you for being a valued member of our gym!</p>

                            <p>Best regards,<br>
                            <strong>Gym Management Team</strong></p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                            <p>If you have questions, please contact us directly.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(member.Email, subject, body);
        }

        public async Task<bool> TestEmailConfigurationAsync()
        {
            try
            {
                var gmailAddress = await SecureStorage.Default.GetAsync("gmail_address");
                var gmailAppPassword = await SecureStorage.Default.GetAsync("gmail_app_password");

                if (string.IsNullOrEmpty(gmailAddress) || string.IsNullOrEmpty(gmailAppPassword))
                {
                    return false;
                }

                // Try to send a test email to the configured address
                var testSubject = "Test Email - Gym Management System";
                var testBody = @"
                    <html>
                    <body>
                        <h2>Email Configuration Test</h2>
                        <p>This is a test email to verify your Gmail configuration is working correctly.</p>
                        <p>If you received this email, your email service is properly configured!</p>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(gmailAddress, testSubject, testBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error testing email configuration: {ex.Message}");
                return false;
            }
        }
    }
}

