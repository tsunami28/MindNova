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
}
