using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public class TimelineService : ITimelineService
{
    private readonly IEnumerable<ITimelineEventSource> _sources;

    public TimelineService(IEnumerable<ITimelineEventSource> sources)
    {
        _sources = sources;
    }

    public async Task<(List<TimelineEvent> Items, int TotalCount)> GetTimelineAsync(Guid clientId, int page, int pageSize)
    {
        var allEvents = new List<TimelineEvent>();

        foreach (var source in _sources)
        {
            var events = await source.GetEventsAsync(clientId);
            allEvents.AddRange(events);
        }

        allEvents.Sort((a, b) => b.Date.CompareTo(a.Date));

        var totalCount = allEvents.Count;
        var items = allEvents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount);
    }
}
