using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Domain.Entities;
using MindNova.Domain.Validation;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Route("api/availability")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AvailabilityController(IAvailabilityService availabilityService, UserManager<ApplicationUser> userManager)
    {
        _availabilityService = availabilityService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAvailabilityBlockRequest request)
    {
        var therapist = await _userManager.FindByIdAsync(request.TherapistUserId);
        if (therapist == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "TherapistUserId does not reference an existing user.",
                Status = 400
            });
        }

        if (!TimeSpan.TryParse(request.StartTime, out var startTime) ||
            !TimeSpan.TryParse(request.EndTime, out var endTime))
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "StartTime and EndTime must be valid time values (HH:mm:ss).",
                Status = 400
            });
        }

        var block = new AvailabilityBlock
        {
            TherapistUserId = request.TherapistUserId,
            DayOfWeek = request.DayOfWeek.HasValue ? (DayOfWeek)request.DayOfWeek.Value : null,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsRecurring = request.IsRecurring,
            SpecificDate = request.SpecificDate
        };

        var validationErrors = AvailabilityBlockValidator.Validate(block);
        if (validationErrors.Count > 0)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join("; ", validationErrors),
                Status = 400
            });
        }

        var created = await _availabilityService.CreateAsync(block);
        return Ok(MapToResponse(created));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "therapist_id")] string therapistId)
    {
        var blocks = await _availabilityService.ListByTherapistAsync(therapistId);
        return Ok(blocks.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var block = await _availabilityService.GetByIdAsync(id);
        if (block == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Availability block not found",
                Detail = $"No availability block with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(block));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAvailabilityBlockRequest request)
    {
        if (!TimeSpan.TryParse(request.StartTime, out var startTime) ||
            !TimeSpan.TryParse(request.EndTime, out var endTime))
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "StartTime and EndTime must be valid time values (HH:mm:ss).",
                Status = 400
            });
        }

        var updated = new AvailabilityBlock
        {
            DayOfWeek = request.DayOfWeek.HasValue ? (DayOfWeek)request.DayOfWeek.Value : null,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsRecurring = request.IsRecurring,
            SpecificDate = request.SpecificDate
        };

        var result = await _availabilityService.UpdateAsync(id, updated);
        if (result == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Availability block not found",
                Detail = $"No availability block with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _availabilityService.DeleteAsync(id);
        if (!deleted)
        {
            return Ok(new ProblemDetails
            {
                Title = "Availability block not found",
                Detail = $"No availability block with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(new { Id = id, Deleted = true });
    }

    private static AvailabilityBlockResponse MapToResponse(AvailabilityBlock block)
    {
        return new AvailabilityBlockResponse
        {
            Id = block.Id,
            TherapistUserId = block.TherapistUserId,
            DayOfWeek = block.DayOfWeek.HasValue ? (int)block.DayOfWeek.Value : null,
            StartTime = block.StartTime.ToString(@"hh\:mm\:ss"),
            EndTime = block.EndTime.ToString(@"hh\:mm\:ss"),
            EffectiveFrom = block.EffectiveFrom,
            EffectiveTo = block.EffectiveTo,
            IsRecurring = block.IsRecurring,
            SpecificDate = block.SpecificDate,
            CreatedAt = block.CreatedAt,
            UpdatedAt = block.UpdatedAt
        };
    }
}
