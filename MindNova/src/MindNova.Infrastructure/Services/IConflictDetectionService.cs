namespace MindNova.Infrastructure.Services;

public interface IConflictDetectionService
{
    Task<string> CheckConflictsAsync(string therapistUserId, DateTime scheduledAt, int durationMinutes, Guid? excludeSessionId = null);
}
