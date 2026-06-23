using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class ConflictDetectionService : IConflictDetectionService
{
    private readonly MindNovaDbContext _context;

    public ConflictDetectionService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<string> CheckConflictsAsync(string therapistUserId, DateTime scheduledAt, int durationMinutes, Guid? excludeSessionId = null)
    {
        var proposedEnd = scheduledAt.AddMinutes(durationMinutes);

        var overlapError = await CheckSessionOverlapAsync(therapistUserId, scheduledAt, proposedEnd, excludeSessionId);
        if (overlapError != null)
            return overlapError;

        var availabilityError = await CheckAvailabilityAsync(therapistUserId, scheduledAt, proposedEnd);
        if (availabilityError != null)
            return availabilityError;

        return null;
    }

    private async Task<string> CheckSessionOverlapAsync(string therapistUserId, DateTime proposedStart, DateTime proposedEnd, Guid? excludeSessionId)
    {
        var query = _context.Sessions
            .Where(s => s.TherapistUserId == therapistUserId)
            .Where(s => s.Status != SessionStatus.Cancelled);

        if (excludeSessionId.HasValue)
            query = query.Where(s => s.Id != excludeSessionId.Value);

        var hasOverlap = await query.AnyAsync(s =>
            s.ScheduledAt < proposedEnd &&
            s.ScheduledAt.AddMinutes(s.DurationMinutes) > proposedStart);

        return hasOverlap ? "The proposed session overlaps an existing session for this therapist." : null;
    }

    private async Task<string> CheckAvailabilityAsync(string therapistUserId, DateTime proposedStart, DateTime proposedEnd)
    {
        var proposedDate = proposedStart.Date;
        var proposedDayOfWeek = proposedStart.DayOfWeek;
        var proposedStartTime = proposedStart.TimeOfDay;
        var proposedEndTime = proposedEnd.TimeOfDay;

        var blocks = await _context.AvailabilityBlocks
            .Where(a => a.TherapistUserId == therapistUserId)
            .ToListAsync();

        var coversProposedTime = blocks.Any(block =>
        {
            if (block.IsRecurring)
            {
                return block.DayOfWeek == proposedDayOfWeek
                    && block.StartTime <= proposedStartTime
                    && block.EndTime >= proposedEndTime
                    && block.EffectiveFrom.Date <= proposedDate
                    && (!block.EffectiveTo.HasValue || block.EffectiveTo.Value.Date >= proposedDate);
            }
            else
            {
                return block.SpecificDate.HasValue
                    && block.SpecificDate.Value.Date == proposedDate
                    && block.StartTime <= proposedStartTime
                    && block.EndTime >= proposedEndTime;
            }
        });

        return coversProposedTime ? null : "No availability block covers the proposed session time for this therapist.";
    }
}
