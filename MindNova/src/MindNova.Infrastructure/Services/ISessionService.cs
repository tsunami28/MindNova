using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface ISessionService
{
    Task<Session> CreateAsync(Session session);
    Task<Session> GetByIdAsync(Guid id);
    Task<(List<Session> Items, int TotalCount)> ListAsync(int page, int pageSize);
    Task<(Session Session, string Error)> UpdateAsync(Guid id, Session updated, SessionStatus? newStatus);
}
