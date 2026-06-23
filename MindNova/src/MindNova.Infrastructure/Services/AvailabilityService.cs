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

    public async Task<AvailabilityBlock> CreateAsync(AvailabilityBlock block)
    {
        block.Id = Guid.NewGuid();
        block.CreatedAt = DateTime.UtcNow;
        block.UpdatedAt = DateTime.UtcNow;

        _context.AvailabilityBlocks.Add(block);
        await _context.SaveChangesAsync();

        return block;
    }

    public async Task<List<AvailabilityBlock>> ListByTherapistAsync(string therapistUserId)
    {
        return await _context.AvailabilityBlocks
            .Where(a => a.TherapistUserId == therapistUserId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<AvailabilityBlock> GetByIdAsync(Guid id)
    {
        return await _context.AvailabilityBlocks.FindAsync(id);
    }

    public async Task<AvailabilityBlock> UpdateAsync(Guid id, AvailabilityBlock updated)
    {
        var block = await _context.AvailabilityBlocks.FindAsync(id);
        if (block == null)
            return null;

        block.DayOfWeek = updated.DayOfWeek;
        block.StartTime = updated.StartTime;
        block.EndTime = updated.EndTime;
        block.EffectiveFrom = updated.EffectiveFrom;
        block.EffectiveTo = updated.EffectiveTo;
        block.IsRecurring = updated.IsRecurring;
        block.SpecificDate = updated.SpecificDate;
        block.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return block;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var block = await _context.AvailabilityBlocks.FindAsync(id);
        if (block == null)
            return false;

        _context.AvailabilityBlocks.Remove(block);
        await _context.SaveChangesAsync();

        return true;
    }
}
