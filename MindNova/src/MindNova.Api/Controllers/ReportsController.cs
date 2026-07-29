using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("api/reports/practice-stats")]
    public async Task<IActionResult> GetPracticeStats(
        [FromQuery(Name = "date_from")] DateTime dateFrom,
        [FromQuery(Name = "date_to")] DateTime dateTo)
    {
        if (dateFrom == default || dateTo == default)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Both date_from and date_to query parameters are required.",
                Status = 400
            });
        }

        if (dateTo < dateFrom)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "date_to must be on or after date_from.",
                Status = 400
            });
        }

        var stats = await _reportService.GetPracticeStatsAsync(dateFrom, dateTo);

        return Ok(new PracticeStatsResponse
        {
            DateFrom = stats.DateFrom,
            DateTo = stats.DateTo,
            TotalSessions = stats.TotalSessions,
            CompletedCount = stats.CompletedCount,
            CancelledCount = stats.CancelledCount,
            NoShowCount = stats.NoShowCount,
            NoShowRate = stats.NoShowRate,
            NewClientsCount = stats.NewClientsCount,
            TherapistUtilisation = stats.TherapistUtilisation.Select(t => new TherapistUtilisationEntry
            {
                TherapistUserId = t.TherapistUserId,
                SessionCount = t.SessionCount
            }).ToList()
        });
    }

    [HttpGet("api/reports/therapist-stats")]
    public async Task<IActionResult> GetTherapistStats(
        [FromQuery(Name = "date_from")] DateTime dateFrom,
        [FromQuery(Name = "date_to")] DateTime dateTo)
    {
        if (dateFrom == default || dateTo == default)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Both date_from and date_to query parameters are required.",
                Status = 400
            });
        }

        if (dateTo < dateFrom)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "date_to must be on or after date_from.",
                Status = 400
            });
        }

        var result = await _reportService.GetTherapistStatsAsync(dateFrom, dateTo);

        return Ok(new TherapistStatsResponse
        {
            DateFrom = result.DateFrom,
            DateTo = result.DateTo,
            Items = result.Items.Select(e => new TherapistStatEntryResponse
            {
                TherapistUserId = e.TherapistUserId,
                TherapistName = e.TherapistName,
                TotalSessions = e.TotalSessions,
                CompletedCount = e.CompletedCount,
                NoShowCount = e.NoShowCount,
                CancelledCount = e.CancelledCount,
                AvailableSlotCount = e.AvailableSlotCount,
                UtilisationRate = e.UtilisationRate
            }).ToList()
        });
    }

    [HttpGet("api/reports/practice-stats/export")]
    public async Task<IActionResult> ExportPracticeStats(
        [FromQuery(Name = "date_from")] DateTime dateFrom,
        [FromQuery(Name = "date_to")] DateTime dateTo)
    {
        if (dateFrom == default || dateTo == default)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Both date_from and date_to query parameters are required.",
                Status = 400
            });
        }

        var stats = await _reportService.GetPracticeStatsAsync(dateFrom, dateTo);

        var sb = new StringBuilder();
        sb.AppendLine("DateFrom,DateTo,TotalSessions,CompletedCount,CancelledCount,NoShowCount,NoShowRate,NewClientsCount");
        sb.AppendLine($"{stats.DateFrom:yyyy-MM-dd},{stats.DateTo:yyyy-MM-dd},{stats.TotalSessions},{stats.CompletedCount},{stats.CancelledCount},{stats.NoShowCount},{stats.NoShowRate},{stats.NewClientsCount}");

        var filename = $"practice-stats-{dateFrom:yyyy-MM-dd}-to-{dateTo:yyyy-MM-dd}.csv";
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", filename);
    }

    [HttpGet("api/reports/therapist-stats/export")]
    public async Task<IActionResult> ExportTherapistStats(
        [FromQuery(Name = "date_from")] DateTime dateFrom,
        [FromQuery(Name = "date_to")] DateTime dateTo)
    {
        if (dateFrom == default || dateTo == default)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Both date_from and date_to query parameters are required.",
                Status = 400
            });
        }

        var result = await _reportService.GetTherapistStatsAsync(dateFrom, dateTo);

        var sb = new StringBuilder();
        sb.AppendLine("TherapistUserId,TherapistName,TotalSessions,CompletedCount,NoShowCount,CancelledCount,AvailableSlotCount,UtilisationRate");
        foreach (var e in result.Items)
        {
            sb.AppendLine($"{e.TherapistUserId},{EscapeCsv(e.TherapistName)},{e.TotalSessions},{e.CompletedCount},{e.NoShowCount},{e.CancelledCount},{e.AvailableSlotCount},{e.UtilisationRate}");
        }

        var filename = $"therapist-stats-{dateFrom:yyyy-MM-dd}-to-{dateTo:yyyy-MM-dd}.csv";
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", filename);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
