using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindNova.Api.Contracts;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Services;

namespace MindNova.Api.Controllers;

[ApiController]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly ITreatmentNoteService _noteService;

    public NotesController(ITreatmentNoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost("api/sessions/{session_id:guid}/notes")]
    public async Task<IActionResult> Create(Guid session_id, [FromBody] CreateNoteRequest request)
    {
        if (request.ProgressRating < 1 || request.ProgressRating > 10)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "ProgressRating must be between 1 and 10.",
                Status = 400
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var note = new TreatmentNote
        {
            PresentingIssue = request.PresentingIssue,
            Interventions = request.Interventions,
            Homework = request.Homework,
            ProgressRating = request.ProgressRating,
            FreeText = request.FreeText
        };

        var (created, error) = await _noteService.CreateAsync(session_id, userId, isAdmin, note);
        return HandleResult(created, error);
    }

    [HttpGet("api/sessions/{session_id:guid}/notes/{id:guid}")]
    public async Task<IActionResult> GetById(Guid session_id, Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var (note, error) = await _noteService.GetByIdAsync(session_id, id, userId, isAdmin);
        return HandleResult(note, error);
    }

    [HttpGet("api/sessions/{session_id:guid}/notes")]
    public async Task<IActionResult> ListBySession(Guid session_id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var (notes, error) = await _noteService.ListBySessionAsync(session_id, userId, isAdmin);

        if (error == "not found")
            return Ok(new ProblemDetails { Title = "Session not found", Detail = $"No session with ID {session_id} exists.", Status = 404 });

        if (error == "forbidden")
            return Ok(new ProblemDetails { Title = "Access denied", Detail = "You are not authorised to view notes for this session.", Status = 403 });

        return Ok(notes.Select(MapToResponse).ToList());
    }

    [HttpPut("api/notes/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteRequest request)
    {
        if (request.ProgressRating < 1 || request.ProgressRating > 10)
        {
            return Ok(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "ProgressRating must be between 1 and 10.",
                Status = 400
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var updated = new TreatmentNote
        {
            PresentingIssue = request.PresentingIssue,
            Interventions = request.Interventions,
            Homework = request.Homework,
            ProgressRating = request.ProgressRating,
            FreeText = request.FreeText
        };

        var (note, error) = await _noteService.UpdateAsync(id, updated, userId, isAdmin);
        return HandleResult(note, error);
    }

    private IActionResult HandleResult(TreatmentNote note, string error)
    {
        if (error == "not found")
            return Ok(new ProblemDetails { Title = "Not found", Detail = "The requested resource was not found.", Status = 404 });

        if (error == "forbidden")
            return Ok(new ProblemDetails { Title = "Access denied", Detail = "You are not authorised to access this note.", Status = 403 });

        if (error != null)
            return Ok(new ProblemDetails { Title = "Error", Detail = error, Status = 400 });

        return Ok(MapToResponse(note));
    }

    private static TreatmentNoteResponse MapToResponse(TreatmentNote note)
    {
        return new TreatmentNoteResponse
        {
            Id = note.Id,
            SessionId = note.SessionId,
            TherapistUserId = note.TherapistUserId,
            PresentingIssue = note.PresentingIssue,
            Interventions = note.Interventions,
            Homework = note.Homework,
            ProgressRating = note.ProgressRating,
            FreeText = note.FreeText,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            IsDeleted = note.IsDeleted
        };
    }
}
