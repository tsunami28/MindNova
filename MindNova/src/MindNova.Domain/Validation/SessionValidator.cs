using MindNova.Domain.Entities;

namespace MindNova.Domain.Validation;

public static class SessionValidator
{
    public static List<string> Validate(Session session)
    {
        var errors = new List<string>();

        if (session.ClientId == Guid.Empty)
            errors.Add("ClientId is required.");

        if (string.IsNullOrWhiteSpace(session.TherapistUserId))
            errors.Add("TherapistUserId is required.");

        if (session.DurationMinutes <= 0)
            errors.Add("DurationMinutes must be greater than zero.");

        return errors;
    }
}
