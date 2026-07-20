using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface ITherapistService
{
    Task<(TherapistProfile Profile, string Error)> CreateAsync(string userId, List<string> specialisations, int maxCaseload);
    Task<TherapistProfile> GetByIdAsync(Guid id);
    Task<(List<TherapistProfile> Items, int TotalCount)> ListAsync(int page, int pageSize, bool includeInactive);
    Task<(TherapistProfile Profile, string Error)> UpdateAsync(Guid id, List<string> specialisations, int maxCaseload);
    Task<TherapistProfile> DeactivateAsync(Guid id);
}
