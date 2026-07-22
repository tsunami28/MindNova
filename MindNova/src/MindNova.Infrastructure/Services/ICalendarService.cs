using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface ICalendarService
{
    Task<(List<CalendarEntry> Items, string Error)> GetCalendarAsync(Guid therapistProfileId, DateTime dateFrom, DateTime dateTo);
}
