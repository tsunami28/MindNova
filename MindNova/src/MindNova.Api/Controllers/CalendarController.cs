using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet("api/therapists/{therapist_id:guid}/calendar")]
    public async Task<IActionResult> GetCalendar(
        Guid therapist_id,
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

        var (items, error) = await _calendarService.GetCalendarAsync(therapist_id, dateFrom, dateTo);

        if (error == "not found")
        {
            return Ok(new ProblemDetails
            {
                Title = "Therapist profile not found",
                Detail = $"No therapist profile with ID {therapist_id} exists.",
                Status = 404
            });
        }

        if (error != null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = error,
                Status = 400
            });
        }

        return Ok(new CalendarResponse
        {
            Items = items.Select(e => new CalendarEntryResponse
            {
                Date = e.Date,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                EntryType = e.EntryType,
                SourceId = e.SourceId
            }).ToList(),
            TherapistProfileId = therapist_id,
            DateFrom = dateFrom,
            DateTo = dateTo
        });
    }
}
