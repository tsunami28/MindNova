namespace MindNova.Api.Contracts;

public class UpdateAvailabilityRequest
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
