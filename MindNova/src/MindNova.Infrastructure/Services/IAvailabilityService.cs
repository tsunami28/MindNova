using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface IAvailabilityService
{
    Task<(AvailabilitySlot Slot, string Error)> CreateAsync(Guid therapistProfileId, AvailabilitySlot slot);
    Task<List<AvailabilitySlot>> ListAsync(Guid therapistProfileId, DateTime? dateFrom, DateTime? dateTo);
    Task<(AvailabilitySlot Slot, string Error)> UpdateAsync(Guid id, TimeSpan startTime, TimeSpan endTime);
    Task<AvailabilitySlot> DeleteAsync(Guid id);
}
