using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Route("api/therapists")]
[Authorize]
public class TherapistsController : ControllerBase
{
    private readonly ITherapistService _therapistService;

    public TherapistsController(ITherapistService therapistService)
    {
        _therapistService = therapistService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTherapistRequest request)
    {
        var (profile, error) = await _therapistService.CreateAsync(
            request.UserId, request.Specialisations, request.MaxCaseload);

        if (error != null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = error,
                Status = 400
            });
        }

        return Ok(MapToResponse(profile));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var profile = await _therapistService.GetByIdAsync(id);
        if (profile == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Therapist profile not found",
                Detail = $"No therapist profile with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(profile));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        [FromQuery(Name = "include_inactive")] bool includeInactive = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _therapistService.ListAsync(page, pageSize, includeInactive);

        return Ok(new PagedResponse<TherapistProfileResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTherapistRequest request)
    {
        var (profile, error) = await _therapistService.UpdateAsync(
            id, request.Specialisations, request.MaxCaseload);

        if (error != null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Therapist profile not found",
                Detail = error,
                Status = 404
            });
        }

        return Ok(MapToResponse(profile));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var profile = await _therapistService.DeactivateAsync(id);
        if (profile == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Therapist profile not found",
                Detail = $"No therapist profile with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(profile));
    }

    private static TherapistProfileResponse MapToResponse(TherapistProfile profile)
    {
        return new TherapistProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Specialisations = profile.Specialisations,
            MaxCaseload = profile.MaxCaseload,
            IsActive = profile.IsActive,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
