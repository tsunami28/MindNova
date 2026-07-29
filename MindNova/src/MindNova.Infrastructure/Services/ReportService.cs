using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly MindNovaDbContext _context;

    public ReportService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<PracticeStats> GetPracticeStatsAsync(DateTime dateFrom, DateTime dateTo)
    {
        var dateToEnd = dateTo.Date.AddDays(1);

        var sessions = await _context.Sessions
            .Where(s => s.ScheduledAt >= dateFrom && s.ScheduledAt < dateToEnd)
            .ToListAsync();

        var totalSessions = sessions.Count;
        var completedCount = sessions.Count(s => s.Status == SessionStatus.Completed);
        var cancelledCount = sessions.Count(s => s.Status == SessionStatus.Cancelled);
        var noShowCount = sessions.Count(s => s.Status == SessionStatus.NoShow);
        var noShowRate = totalSessions > 0 ? (double)noShowCount / totalSessions * 100 : 0;

        var newClientsCount = await _context.Clients
            .CountAsync(c => c.CreatedAt >= dateFrom && c.CreatedAt < dateToEnd);

        var therapistUtilisation = sessions
            .GroupBy(s => s.TherapistUserId)
            .Select(g => new TherapistSessionCount
            {
                TherapistUserId = g.Key,
                SessionCount = g.Count()
            })
            .ToList();

        return new PracticeStats
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalSessions = totalSessions,
            CompletedCount = completedCount,
            CancelledCount = cancelledCount,
            NoShowCount = noShowCount,
            NoShowRate = Math.Round(noShowRate, 1),
            NewClientsCount = newClientsCount,
            TherapistUtilisation = therapistUtilisation
        };
    }

    public async Task<TherapistStatsResult> GetTherapistStatsAsync(DateTime dateFrom, DateTime dateTo)
    {
        var dateToEnd = dateTo.Date.AddDays(1);

        var sessions = await _context.Sessions
            .Where(s => s.ScheduledAt >= dateFrom && s.ScheduledAt < dateToEnd)
            .ToListAsync();

        var therapistIds = sessions.Select(s => s.TherapistUserId).Distinct().ToList();

        if (therapistIds.Count == 0)
        {
            return new TherapistStatsResult
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Items = new List<TherapistStatEntry>()
            };
        }

        var profiles = await _context.TherapistProfiles
            .Where(p => therapistIds.Contains(p.UserId))
            .ToListAsync();

        var slots = await _context.AvailabilitySlots
            .Where(a => profiles.Select(p => p.Id).Contains(a.TherapistProfileId))
            .ToListAsync();

        var users = await _context.Users
            .Where(u => therapistIds.Contains(u.Id))
            .ToListAsync();

        var items = therapistIds.Select(therapistUserId =>
        {
            var therapistSessions = sessions.Where(s => s.TherapistUserId == therapistUserId).ToList();
            var profile = profiles.FirstOrDefault(p => p.UserId == therapistUserId);
            var user = users.FirstOrDefault(u => u.Id == therapistUserId);

            var slotCount = 0;
            if (profile != null)
            {
                var therapistSlots = slots.Where(a => a.TherapistProfileId == profile.Id).ToList();
                foreach (var slot in therapistSlots)
                {
                    if (slot.IsRecurring && slot.DayOfWeek.HasValue)
                    {
                        for (var date = dateFrom.Date; date < dateToEnd; date = date.AddDays(1))
                        {
                            if ((int)date.DayOfWeek == slot.DayOfWeek.Value)
                                slotCount++;
                        }
                    }
                    else if (!slot.IsRecurring && slot.SpecificDate.HasValue
                             && slot.SpecificDate.Value.Date >= dateFrom.Date
                             && slot.SpecificDate.Value.Date < dateToEnd)
                    {
                        slotCount++;
                    }
                }
            }

            var total = therapistSessions.Count;
            var utilisationRate = slotCount > 0 ? Math.Round((double)total / slotCount * 100, 1) : 0;

            return new TherapistStatEntry
            {
                TherapistUserId = therapistUserId,
                TherapistName = user?.Email ?? therapistUserId,
                TotalSessions = total,
                CompletedCount = therapistSessions.Count(s => s.Status == SessionStatus.Completed),
                NoShowCount = therapistSessions.Count(s => s.Status == SessionStatus.NoShow),
                CancelledCount = therapistSessions.Count(s => s.Status == SessionStatus.Cancelled),
                AvailableSlotCount = slotCount,
                UtilisationRate = utilisationRate
            };
        }).ToList();

        return new TherapistStatsResult
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Items = items
        };
    }
}
