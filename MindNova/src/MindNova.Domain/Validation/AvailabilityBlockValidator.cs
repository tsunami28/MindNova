using MindNova.Domain.Entities;

namespace MindNova.Domain.Validation;

public static class AvailabilityBlockValidator
{
    public static List<string> Validate(AvailabilityBlock block)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(block.TherapistUserId))
            errors.Add("TherapistUserId is required.");

        if (block.StartTime >= block.EndTime)
            errors.Add("StartTime must be before EndTime.");

        if (block.IsRecurring && !block.DayOfWeek.HasValue)
            errors.Add("A recurring block must have a DayOfWeek.");

        return errors;
    }
}
