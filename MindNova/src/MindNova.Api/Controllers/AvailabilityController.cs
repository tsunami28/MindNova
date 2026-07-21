using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpPost("api/therapists/{therapistId:guid}/availability")]
    public async Task<IActionResult> Create(Guid therapistId, [FromBody] CreateAvailabilityRequest request)
    {
        var slot = new AvailabilitySlot
        {
            DayOfWeek = request.DayOfWeek,
            SpecificDate = request.SpecificDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsRecurring = request.IsRecurring
        };

        var (created, error) = await _availabilityService.CreateAsync(therapistId, slot);
        if (error != null)
        {
            var status = error.Contains("No active") ? 404 : 400;
            return Ok(new ProblemDetails
            {
                Title = status == 404 ? "Therapist profile not found" : "Overlap detected",
                Detail = error,
                Status = status
            });
        }

        return Ok(MapToResponse(created));
    }

    [HttpGet("api/therapists/{therapistId:guid}/availability")]
    public async Task<IActionResult> List(
        Guid therapistId,
        [FromQuery(Name = "date_from")] DateTime? dateFrom = null,
        [FromQuery(Name = "date_to")] DateTime? dateTo = null)
    {
        var slots = await _availabilityService.ListAsync(therapistId, dateFrom, dateTo);
        return Ok(slots.Select(MapToResponse).ToList());
    }

    [HttpPut("api/availability/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAvailabilityRequest request)
    {
        var (slot, error) = await _availabilityService.UpdateAsync(id, request.StartTime, request.EndTime);
        if (error != null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Update failed",
                Detail = error,
                Status = error.Contains("No availability") ? 404 : 400
            });
        }

        return Ok(MapToResponse(slot));
    }

    [HttpDelete("api/availability/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var slot = await _availabilityService.DeleteAsync(id);
        if (slot == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Availability slot not found",
                Detail = $"No availability slot with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(slot));
    }

    private static AvailabilitySlotResponse MapToResponse(AvailabilitySlot slot)
    {
        return new AvailabilitySlotResponse
        {
            Id = slot.Id,
            TherapistProfileId = slot.TherapistProfileId,
            DayOfWeek = slot.DayOfWeek,
            SpecificDate = slot.SpecificDate,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            IsRecurring = slot.IsRecurring,
            CreatedAt = slot.CreatedAt,
            UpdatedAt = slot.UpdatedAt
        };
    }
}
