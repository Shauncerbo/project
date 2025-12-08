using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using project.Data;
using project.Models;

namespace project.Services
{
    public class ExpirationNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpirationNotificationService>? _logger;

        public ExpirationNotificationService(
            IServiceProvider serviceProvider,
            ILogger<ExpirationNotificationService>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("✅ Expiration Notification Service started - Running automatically in background");

            // Run immediately on startup
            try
            {
                _logger?.LogInformation("Running initial expiration check on startup...");
                await CheckAndNotifyExpiringMemberships();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in initial expiration check");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate next run time (9:00 AM daily)
                    var now = DateTime.Now;
                    var nextRun = now.Date.AddDays(1).AddHours(9);
                    if (nextRun <= now)
                    {
                        nextRun = nextRun.AddDays(1);
                    }
                    var delay = nextRun - now;

                    _logger?.LogInformation($"⏰ Next automatic expiration check scheduled for: {nextRun:yyyy-MM-dd HH:mm:ss}");

                    await Task.Delay(delay, stoppingToken);

                    // After delay, run the check
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await CheckAndNotifyExpiringMemberships();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogInformation("Expiration Notification Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in expiration notification service");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger?.LogInformation("Expiration Notification Service stopped");
        }

        private async Task CheckAndNotifyExpiringMemberships()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var today = DateTime.Today;
            var sevenDaysFromNow = today.AddDays(7);

            _logger?.LogInformation($"Checking for memberships expiring between {today:yyyy-MM-dd} and {sevenDaysFromNow:yyyy-MM-dd}");

            // Get active members expiring within 7 days
            var expiringMembers = await context.Members
                .Include(m => m.MembershipType)
                .Where(m => !m.IsArchived &&
                           m.Status == "Active" &&
                           m.ExpirationDate.HasValue &&
                           m.ExpirationDate >= today &&
                           m.ExpirationDate <= sevenDaysFromNow &&
                           !string.IsNullOrEmpty(m.Email))
                .ToListAsync();

            _logger?.LogInformation($"Found {expiringMembers.Count} members with expiring memberships");

            int notificationsSent = 0;
            int notificationsSkipped = 0;

            foreach (var member in expiringMembers)
            {
                if (member.ExpirationDate.HasValue)
                {
                    var daysUntilExpiration = (member.ExpirationDate.Value.Date - today).Days;

                    // Send notifications at 7 days, 3 days, and 1 day before expiration
                    if (daysUntilExpiration == 7 || daysUntilExpiration == 3 || daysUntilExpiration == 1)
                    {
                        try
                        {
                            // Check if notification already sent today for this member and day count
                            var todayStart = DateTime.Today;
                            var todayEnd = todayStart.AddDays(1);

                            var alreadyNotified = await context.Notifications
                                .AnyAsync(n => n.MemberID == member.MemberID &&
                                             n.Message.Contains($"expires in {daysUntilExpiration}") &&
                                             n.DateSent >= todayStart &&
                                             n.DateSent < todayEnd);

                            if (!alreadyNotified)
                            {
                                // Send email
                                var emailSent = await emailService.SendExpirationReminderAsync(member, daysUntilExpiration);

                                if (emailSent)
                                {
                                    // Save notification to database
                                    var notification = new Notification
                                    {
                                        MemberID = member.MemberID,
                                        Message = $"Membership expires in {daysUntilExpiration} day{(daysUntilExpiration > 1 ? "s" : "")}",
                                        DateSent = DateTime.Now
                                    };
                                    context.Notifications.Add(notification);
                                    await context.SaveChangesAsync();

                                    notificationsSent++;
                                    _logger?.LogInformation(
                                        $"✅ Sent expiration reminder to {member.Email} (Member: {member.FirstName} {member.LastName}, {daysUntilExpiration} days remaining)");
                                }
                                else
                                {
                                    _logger?.LogWarning(
                                        $"⚠️ Failed to send email to {member.Email} (Member: {member.FirstName} {member.LastName})");
                                }
                            }
                            else
                            {
                                notificationsSkipped++;
                                _logger?.LogInformation(
                                    $"⏭️ Skipped notification for {member.Email} - already notified today ({daysUntilExpiration} days)");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, $"❌ Failed to process notification for member {member.MemberID} ({member.Email})");
                        }
                    }
                }
            }

            _logger?.LogInformation(
                $"Expiration check completed: {notificationsSent} notifications sent, {notificationsSkipped} skipped");
        }
    }
}

