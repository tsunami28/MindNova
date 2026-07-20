using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface ITimelineEventSource
{
    Task<List<TimelineEvent>> GetEventsAsync(Guid clientId);
}
