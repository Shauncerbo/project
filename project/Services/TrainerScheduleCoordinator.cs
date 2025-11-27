using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using project.Data;
using project.Models;

namespace project.Services
{
    /// <summary>
    /// Centralizes trainer schedule locking / releasing logic shared by Members and Walk-ins.
    /// </summary>
    public class TrainerScheduleCoordinator
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public TrainerScheduleCoordinator(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Releases trainer schedules whose associated session dates are already in the past.
        /// This allows the same schedule block to become available again for future bookings.
        /// </summary>
        public async Task ReleaseCompletedScheduleBlocksAsync(DateTime? referenceDate = null)
        {
            // Use a short-lived DbContext instance created from the factory.
            // This avoids running multiple operations on the same DbContext instance
            // from different components/threads.
            await using var context = await _contextFactory.CreateDbContextAsync();

            var today = (referenceDate ?? DateTime.Today).Date;
            var scheduleIdsToFree = new HashSet<int>();

            var membersToUpdate = await context.Members
                .Where(m => !m.IsArchived
                    && m.TrainerScheduleID.HasValue
                    && m.JoinDate.Date < today)
                .ToListAsync();

            foreach (var member in membersToUpdate)
            {
                if (member.TrainerScheduleID.HasValue)
                {
                    scheduleIdsToFree.Add(member.TrainerScheduleID.Value);
                    member.TrainerScheduleID = null;
                }
            }

            var walkInsToUpdate = await context.WalkIns
                .Where(w => !w.IsArchived
                    && w.TrainerScheduleID.HasValue
                    && w.VisitDate.Date < today)
                .ToListAsync();

            foreach (var walkIn in walkInsToUpdate)
            {
                if (walkIn.TrainerScheduleID.HasValue)
                {
                    scheduleIdsToFree.Add(walkIn.TrainerScheduleID.Value);
                    walkIn.TrainerScheduleID = null;
                }
            }

            if (scheduleIdsToFree.Any())
            {
                var schedules = await context.TrainerSchedules
                    .Where(ts => scheduleIdsToFree.Contains(ts.TrainerScheduleID))
                    .ToListAsync();

                foreach (var schedule in schedules)
                {
                    schedule.IsAvailable = true;
                }
            }

            if (membersToUpdate.Any() || walkInsToUpdate.Any() || scheduleIdsToFree.Any())
            {
                await context.SaveChangesAsync();
            }
        }
    }
}

