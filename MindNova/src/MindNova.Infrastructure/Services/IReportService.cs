using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface IReportService
{
    Task<PracticeStats> GetPracticeStatsAsync(DateTime dateFrom, DateTime dateTo);
}
