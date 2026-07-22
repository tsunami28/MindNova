using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly MindNovaDbContext _context;
    private const int MaxDays = 90;

    public CalendarService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<(List<CalendarEntry> Items, string Error)> GetCalendarAsync(Guid therapistProfileId, DateTime dateFrom, DateTime dateTo)
    {
        var profile = await _context.TherapistProfiles.FindAsync(therapistProfileId);
        if (profile == null)
            return (null, "not found");

        if ((dateTo - dateFrom).TotalDays > MaxDays)
            return (null, $"Date range exceeds the maximum of {MaxDays} days.");

        var entries = new List<CalendarEntry>();

        var availabilityEntries = await BuildAvailabilityEntriesAsync(therapistProfileId, dateFrom, dateTo);
        entries.AddRange(availabilityEntries);

        var sessionEntries = await BuildSessionEntriesAsync(profile.UserId, dateFrom, dateTo);
        entries.AddRange(sessionEntries);

        entries = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ToList();

        return (entries, null);
    }

    private async Task<List<CalendarEntry>> BuildAvailabilityEntriesAsync(Guid therapistProfileId, DateTime dateFrom, DateTime dateTo)
    {
        var slots = await _context.AvailabilitySlots
            .Where(a => a.TherapistProfileId == therapistProfileId)
            .Where(a =>
                a.IsRecurring ||
                (!a.IsRecurring && a.SpecificDate >= dateFrom && a.SpecificDate <= dateTo))
            .ToListAsync();

        var entries = new List<CalendarEntry>();

        foreach (var slot in slots)
        {
            if (slot.IsRecurring && slot.DayOfWeek.HasValue)
            {
                for (var date = dateFrom.Date; date <= dateTo.Date; date = date.AddDays(1))
                {
                    if ((int)date.DayOfWeek == slot.DayOfWeek.Value)
                    {
                        entries.Add(new CalendarEntry
                        {
                            Date = date,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            EntryType = "Availability",
                            SourceId = slot.Id
                        });
                    }
                }
            }
            else if (!slot.IsRecurring && slot.SpecificDate.HasValue)
            {
                entries.Add(new CalendarEntry
                {
                    Date = slot.SpecificDate.Value.Date,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    EntryType = "Availability",
                    SourceId = slot.Id
                });
            }
        }

        return entries;
    }

    private async Task<List<CalendarEntry>> BuildSessionEntriesAsync(string therapistUserId, DateTime dateFrom, DateTime dateTo)
    {
        var sessions = await _context.Sessions
            .Where(s => s.TherapistUserId == therapistUserId)
            .Where(s => s.ScheduledAt >= dateFrom && s.ScheduledAt <= dateTo.AddDays(1))
            .Where(s => s.Status == SessionStatus.Scheduled)
            .ToListAsync();

        return sessions.Select(s => new CalendarEntry
        {
            Date = s.ScheduledAt.Date,
            StartTime = s.ScheduledAt.TimeOfDay,
            EndTime = s.ScheduledAt.AddMinutes(s.DurationMinutes).TimeOfDay,
            EntryType = "Session",
            SourceId = s.Id
        }).ToList();
    }
}
