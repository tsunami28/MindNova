using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class SessionTimelineEventSource : ITimelineEventSource
{
    private readonly MindNovaDbContext _context;

    public SessionTimelineEventSource(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<List<TimelineEvent>> GetEventsAsync(Guid clientId)
    {
        var sessions = await _context.Sessions
            .Where(s => s.ClientId == clientId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();

        return sessions.Select(s => new TimelineEvent
        {
            Date = s.ScheduledAt,
            EventType = "Session",
            Summary = $"{s.SessionType} - {s.Status}",
            SourceId = s.Id
        }).ToList();
    }
}
