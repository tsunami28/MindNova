using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly MindNovaDbContext _context;

    public SessionService(MindNovaDbContext context)
    {
        _context = context;
    }

    public async Task<Session> CreateAsync(Session session)
    {
        session.Id = Guid.NewGuid();
        session.Status = SessionStatus.Scheduled;
        session.CreatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<Session> GetByIdAsync(Guid id)
    {
        return await _context.Sessions.FindAsync(id);
    }

    public async Task<(List<Session> Items, int TotalCount)> ListAsync(
        Guid? clientId, string therapistId, SessionStatus? status,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var query = _context.Sessions.AsQueryable();

        if (clientId.HasValue)
            query = query.Where(s => s.ClientId == clientId.Value);

        if (!string.IsNullOrWhiteSpace(therapistId))
            query = query.Where(s => s.TherapistUserId == therapistId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (dateFrom.HasValue)
            query = query.Where(s => s.ScheduledAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.ScheduledAt <= dateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(Session Session, string Error)> UpdateAsync(Guid id, Session updated, SessionStatus? newStatus)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
            return (null, "not found");

        if (newStatus.HasValue && newStatus.Value != session.Status)
        {
            var transitionError = ValidateStatusTransition(session.Status, newStatus.Value);
            if (transitionError != null)
                return (null, transitionError);

            session.Status = newStatus.Value;
        }

        session.ScheduledAt = updated.ScheduledAt;
        session.DurationMinutes = updated.DurationMinutes;
        session.SessionType = updated.SessionType;
        session.Notes = updated.Notes;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (session, null);
    }

    private static string ValidateStatusTransition(SessionStatus current, SessionStatus target)
    {
        if (current == SessionStatus.Scheduled)
            return null;

        if (current == SessionStatus.Completed)
            return "Cannot transition from Completed; it is a terminal state.";

        if (current == SessionStatus.Cancelled)
            return "Cannot transition from Cancelled; it is a terminal state.";

        if (current == SessionStatus.NoShow)
            return "Cannot transition from NoShow; it is a terminal state.";

        return $"Invalid status transition from {current} to {target}.";
    }
}
