using MindNova.Domain.Entities;
using MindNova.Infrastructure.Services;
using Moq;

namespace MindNova.Api.Tests.Timeline;

public class TimelineServiceTests
{
    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-10")]
    public async Task GetTimeline_MultipleSources_AggregatesAndSortsByDateDescending()
    {
        var clientId = Guid.NewGuid();

        var sessionSource = new Mock<ITimelineEventSource>();
        sessionSource.Setup(s => s.GetEventsAsync(clientId)).ReturnsAsync(new List<TimelineEvent>
        {
            new() { Date = new DateTime(2026, 7, 10), EventType = "Session", Summary = "Individual - Completed", SourceId = Guid.NewGuid() },
            new() { Date = new DateTime(2026, 7, 5), EventType = "Session", Summary = "FollowUp - Scheduled", SourceId = Guid.NewGuid() }
        });

        var noteSource = new Mock<ITimelineEventSource>();
        noteSource.Setup(s => s.GetEventsAsync(clientId)).ReturnsAsync(new List<TimelineEvent>
        {
            new() { Date = new DateTime(2026, 7, 8), EventType = "Note", Summary = "Progress note added", SourceId = Guid.NewGuid() }
        });

        var service = new TimelineService(new[] { sessionSource.Object, noteSource.Object });

        var (items, totalCount) = await service.GetTimelineAsync(clientId, page: 1, pageSize: 20);

        Assert.Equal(3, totalCount);
        Assert.Equal(3, items.Count);
        Assert.Equal("Session", items[0].EventType);
        Assert.Equal(new DateTime(2026, 7, 10), items[0].Date);
        Assert.Equal("Note", items[1].EventType);
        Assert.Equal(new DateTime(2026, 7, 8), items[1].Date);
        Assert.Equal("Session", items[2].EventType);
        Assert.Equal(new DateTime(2026, 7, 5), items[2].Date);
    }

    [Fact]
    [Trait("Story", "MN-16")]
    [Trait("AC", "AC-10")]
    public async Task GetTimeline_PaginatesAcrossSources()
    {
        var clientId = Guid.NewGuid();

        var source1 = new Mock<ITimelineEventSource>();
        source1.Setup(s => s.GetEventsAsync(clientId)).ReturnsAsync(new List<TimelineEvent>
        {
            new() { Date = new DateTime(2026, 7, 10), EventType = "Session", Summary = "S1", SourceId = Guid.NewGuid() },
            new() { Date = new DateTime(2026, 7, 8), EventType = "Session", Summary = "S2", SourceId = Guid.NewGuid() }
        });

        var source2 = new Mock<ITimelineEventSource>();
        source2.Setup(s => s.GetEventsAsync(clientId)).ReturnsAsync(new List<TimelineEvent>
        {
            new() { Date = new DateTime(2026, 7, 9), EventType = "Note", Summary = "N1", SourceId = Guid.NewGuid() }
        });

        var service = new TimelineService(new[] { source1.Object, source2.Object });

        var (page1, totalCount) = await service.GetTimelineAsync(clientId, page: 1, pageSize: 2);
        var (page2, _) = await service.GetTimelineAsync(clientId, page: 2, pageSize: 2);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, page1.Count);
        Assert.Single(page2);
        Assert.Equal(new DateTime(2026, 7, 10), page1[0].Date);
        Assert.Equal(new DateTime(2026, 7, 9), page1[1].Date);
        Assert.Equal(new DateTime(2026, 7, 8), page2[0].Date);
    }
}
