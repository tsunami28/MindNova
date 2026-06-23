using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface IAvailabilityService
{
    Task<AvailabilityBlock> CreateAsync(AvailabilityBlock block);
    Task<List<AvailabilityBlock>> ListByTherapistAsync(string therapistUserId);
    Task<AvailabilityBlock> GetByIdAsync(Guid id);
    Task<AvailabilityBlock> UpdateAsync(Guid id, AvailabilityBlock updated);
    Task<bool> DeleteAsync(Guid id);
}
