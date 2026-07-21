using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly MindNovaDbContext _context;

    public AvailabilityService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<(AvailabilitySlot Slot, string Error)> CreateAsync(Guid therapistProfileId, AvailabilitySlot slot)
    {
        var profile = await _context.TherapistProfiles.FindAsync(therapistProfileId);
        if (profile == null || !profile.IsActive)
            return (null, $"No active therapist profile with ID {therapistProfileId} exists.");

        var hasOverlap = await CheckOverlapAsync(therapistProfileId, slot, excludeId: null);
        if (hasOverlap)
            return (null, "The proposed slot overlaps an existing availability slot for this therapist.");

        slot.Id = Guid.NewGuid();
        slot.TherapistProfileId = therapistProfileId;
        slot.CreatedAt = DateTime.UtcNow;
        slot.UpdatedAt = DateTime.UtcNow;

        _context.AvailabilitySlots.Add(slot);
        await _context.SaveChangesAsync();

        return (slot, null);
    }

    public async Task<List<AvailabilitySlot>> ListAsync(Guid therapistProfileId, DateTime? dateFrom, DateTime? dateTo)
    {
        var query = _context.AvailabilitySlots
            .Where(a => a.TherapistProfileId == therapistProfileId);

        if (dateFrom.HasValue && dateTo.HasValue)
        {
            var fromDow = (int)dateFrom.Value.DayOfWeek;
            var toDow = (int)dateTo.Value.DayOfWeek;

            query = query.Where(a =>
                (a.IsRecurring) ||
                (!a.IsRecurring && a.SpecificDate >= dateFrom.Value && a.SpecificDate <= dateTo.Value));
        }

        return await query.OrderBy(a => a.IsRecurring ? 0 : 1)
            .ThenBy(a => a.DayOfWeek)
            .ThenBy(a => a.SpecificDate)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<(AvailabilitySlot Slot, string Error)> UpdateAsync(Guid id, TimeSpan startTime, TimeSpan endTime)
    {
        var slot = await _context.AvailabilitySlots.FindAsync(id);
        if (slot == null)
            return (null, $"No availability slot with ID {id} exists.");

        var updatedSlot = new AvailabilitySlot
        {
            DayOfWeek = slot.DayOfWeek,
            SpecificDate = slot.SpecificDate,
            IsRecurring = slot.IsRecurring,
            StartTime = startTime,
            EndTime = endTime
        };

        var hasOverlap = await CheckOverlapAsync(slot.TherapistProfileId, updatedSlot, excludeId: id);
        if (hasOverlap)
            return (null, "The updated times overlap an existing availability slot for this therapist.");

        slot.StartTime = startTime;
        slot.EndTime = endTime;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (slot, null);
    }

    public async Task<AvailabilitySlot> DeleteAsync(Guid id)
    {
        var slot = await _context.AvailabilitySlots.FindAsync(id);
        if (slot == null)
            return null;

        _context.AvailabilitySlots.Remove(slot);
        await _context.SaveChangesAsync();

        return slot;
    }

    private async Task<bool> CheckOverlapAsync(Guid therapistProfileId, AvailabilitySlot proposed, Guid? excludeId)
    {
        var query = _context.AvailabilitySlots
            .Where(a => a.TherapistProfileId == therapistProfileId);

        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);

        if (proposed.IsRecurring)
        {
            return await query.AnyAsync(a =>
                a.IsRecurring &&
                a.DayOfWeek == proposed.DayOfWeek &&
                a.StartTime < proposed.EndTime &&
                a.EndTime > proposed.StartTime);
        }
        else
        {
            return await query.AnyAsync(a =>
                !a.IsRecurring &&
                a.SpecificDate == proposed.SpecificDate &&
                a.StartTime < proposed.EndTime &&
                a.EndTime > proposed.StartTime);
        }
    }
}
