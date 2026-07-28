using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class TreatmentNoteService : ITreatmentNoteService
{
    private readonly MindNovaDbContext _context;

    public TreatmentNoteService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<(TreatmentNote Note, string Error)> CreateAsync(Guid sessionId, string authenticatedUserId, bool isAdmin, TreatmentNote note)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
            return (null, "not found");

        if (!isAdmin && session.TherapistUserId != authenticatedUserId)
            return (null, "forbidden");

        note.Id = Guid.NewGuid();
        note.SessionId = sessionId;
        note.TherapistUserId = authenticatedUserId;
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;
        note.IsDeleted = false;

        _context.TreatmentNotes.Add(note);
        await _context.SaveChangesAsync();

        return (note, null);
    }

    public async Task<(TreatmentNote Note, string Error)> GetByIdAsync(Guid sessionId, Guid noteId, string authenticatedUserId, bool isAdmin)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
            return (null, "not found");

        if (!isAdmin && session.TherapistUserId != authenticatedUserId)
            return (null, "forbidden");

        var note = await _context.TreatmentNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.SessionId == sessionId && !n.IsDeleted);

        if (note == null)
            return (null, "not found");

        return (note, null);
    }

    public async Task<(List<TreatmentNote> Notes, string Error)> ListBySessionAsync(Guid sessionId, string authenticatedUserId, bool isAdmin)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
            return (null, "not found");

        if (!isAdmin && session.TherapistUserId != authenticatedUserId)
            return (null, "forbidden");

        var notes = await _context.TreatmentNotes
            .Where(n => n.SessionId == sessionId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return (notes, null);
    }

    public async Task<(TreatmentNote Note, string Error)> UpdateAsync(Guid noteId, TreatmentNote updated, string authenticatedUserId, bool isAdmin)
    {
        var note = await _context.TreatmentNotes.FindAsync(noteId);
        if (note == null)
            return (null, "not found");

        if (note.IsDeleted)
            return (null, "Cannot update a deleted note.");

        var session = await _context.Sessions.FindAsync(note.SessionId);
        if (session == null)
            return (null, "not found");

        if (!isAdmin && session.TherapistUserId != authenticatedUserId)
            return (null, "forbidden");

        note.PresentingIssue = updated.PresentingIssue;
        note.Interventions = updated.Interventions;
        note.Homework = updated.Homework;
        note.ProgressRating = updated.ProgressRating;
        note.FreeText = updated.FreeText;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (note, null);
    }

    public async Task<(List<TreatmentNote> Notes, int TotalCount, string Error)> ListByClientAsync(Guid clientId, string authenticatedUserId, bool isAdmin, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null)
            return (null, 0, "not found");

        if (!isAdmin)
        {
            var hasAccess = await _context.Sessions
                .AnyAsync(s => s.ClientId == clientId && s.TherapistUserId == authenticatedUserId);
            if (!hasAccess)
                return (null, 0, "forbidden");
        }

        var query = _context.TreatmentNotes
            .Join(_context.Sessions, n => n.SessionId, s => s.Id, (n, s) => new { Note = n, Session = s })
            .Where(x => x.Session.ClientId == clientId && !x.Note.IsDeleted);

        if (dateFrom.HasValue)
            query = query.Where(x => x.Session.ScheduledAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.Session.ScheduledAt <= dateTo.Value.AddDays(1));

        var totalCount = await query.CountAsync();

        var notes = await query
            .OrderByDescending(x => x.Session.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Note)
            .ToListAsync();

        return (notes, totalCount, null);
    }
}
