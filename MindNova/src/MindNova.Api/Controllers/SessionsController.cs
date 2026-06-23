using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Domain.Entities;
using MindNova.Domain.Validation;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IClientService _clientService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SessionsController(
        ISessionService sessionService,
        IClientService clientService,
        UserManager<ApplicationUser> userManager)
    {
        _sessionService = sessionService;
        _clientService = clientService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request)
    {
        var client = await _clientService.GetByIdAsync(request.ClientId);
        if (client == null || client.IsArchived)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "ClientId does not reference an existing non-archived client.",
                Status = 400
            });
        }

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

        if (!Enum.TryParse<SessionType>(request.SessionType, out var sessionType))
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "SessionType is invalid. Valid values: Individual, Group, Intake, FollowUp.",
                Status = 400
            });
        }

        var session = new Session
        {
            ClientId = request.ClientId,
            TherapistUserId = request.TherapistUserId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            SessionType = sessionType,
            Notes = request.Notes
        };

        var validationErrors = SessionValidator.Validate(session);
        if (validationErrors.Count > 0)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join("; ", validationErrors),
                Status = 400
            });
        }

        var created = await _sessionService.CreateAsync(session);
        return Ok(MapToResponse(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session with ID {id} exists.",
                Status = 404
            });
        }

        return Ok(MapToResponse(session));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "client_id")] Guid? clientId = null,
        [FromQuery(Name = "therapist_id")] string therapistId = null,
        [FromQuery(Name = "status")] string status = null,
        [FromQuery(Name = "date_from")] DateTime? dateFrom = null,
        [FromQuery(Name = "date_to")] DateTime? dateTo = null,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        SessionStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SessionStatus>(status, out var parsed))
            statusFilter = parsed;

        var (sessions, totalCount) = await _sessionService.ListAsync(
            clientId, therapistId, statusFilter, dateFrom, dateTo, page, pageSize);

        return Ok(new PagedResponse<SessionResponse>
        {
            Items = sessions.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSessionRequest request)
    {
        if (!Enum.TryParse<SessionType>(request.SessionType, out var sessionType))
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "SessionType is invalid. Valid values: Individual, Group, Intake, FollowUp.",
                Status = 400
            });
        }

        SessionStatus? newStatus = null;
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (!Enum.TryParse<SessionStatus>(request.Status, out var parsedStatus))
            {
                return Ok(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = "Status is invalid. Valid values: Scheduled, Completed, Cancelled, NoShow.",
                    Status = 400
                });
            }
            newStatus = parsedStatus;
        }

        var updated = new Session
        {
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            SessionType = sessionType,
            Notes = request.Notes
        };

        var (result, error) = await _sessionService.UpdateAsync(id, updated, newStatus);

        if (error == "not found")
        {
            return Ok(new ProblemDetails
            {
                Title = "Session not found",
                Detail = $"No session with ID {id} exists.",
                Status = 404
            });
        }

        if (error != null)
        {
            return Ok(new ProblemDetails
            {
                Title = "Invalid status transition",
                Detail = error,
                Status = 400
            });
        }

        return Ok(MapToResponse(result));
    }

    private static SessionResponse MapToResponse(Session session)
    {
        return new SessionResponse
        {
            Id = session.Id,
            ClientId = session.ClientId,
            TherapistUserId = session.TherapistUserId,
            ScheduledAt = session.ScheduledAt,
            DurationMinutes = session.DurationMinutes,
            SessionType = session.SessionType.ToString(),
            Status = session.Status.ToString(),
            Notes = session.Notes,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}
