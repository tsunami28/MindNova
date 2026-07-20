using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface ITimelineService
{
    Task<(List<TimelineEvent> Items, int TotalCount)> GetTimelineAsync(Guid clientId, int page, int pageSize);
}
