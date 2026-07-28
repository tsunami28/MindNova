using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Services;

public interface ITreatmentNoteService
{
    Task<(TreatmentNote Note, string Error)> CreateAsync(Guid sessionId, string authenticatedUserId, bool isAdmin, TreatmentNote note);
    Task<(TreatmentNote Note, string Error)> GetByIdAsync(Guid sessionId, Guid noteId, string authenticatedUserId, bool isAdmin);
    Task<(List<TreatmentNote> Notes, string Error)> ListBySessionAsync(Guid sessionId, string authenticatedUserId, bool isAdmin);
    Task<(TreatmentNote Note, string Error)> UpdateAsync(Guid noteId, TreatmentNote updated, string authenticatedUserId, bool isAdmin);
}
