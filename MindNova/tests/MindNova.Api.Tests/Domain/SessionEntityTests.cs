using MindNova.Domain.Entities;

namespace MindNova.Api.Tests.Domain;

public class SessionEntityTests
{
    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-1")]
    public void Session_HasAllTenProperties_WithCorrectTypes()
    {
        var session = new Session();

        Assert.IsType<Guid>(session.Id);
        Assert.IsType<Guid>(session.ClientId);
        Assert.IsType<string>(session.TherapistUserId);
        Assert.IsType<DateTime>(session.ScheduledAt);
        Assert.IsType<int>(session.DurationMinutes);
        Assert.IsType<SessionType>(session.SessionType);
        Assert.IsType<SessionStatus>(session.Status);
        Assert.IsType<string>(session.Notes);
        Assert.IsType<DateTime>(session.CreatedAt);
        Assert.IsType<DateTime>(session.UpdatedAt);
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-2")]
    public void SessionType_HasExactlyFourMembers()
    {
        var values = Enum.GetValues<SessionType>();

        Assert.Equal(4, values.Length);
        Assert.Contains(SessionType.Individual, values);
        Assert.Contains(SessionType.Group, values);
        Assert.Contains(SessionType.Intake, values);
        Assert.Contains(SessionType.FollowUp, values);
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-3")]
    public void SessionStatus_HasExactlyFourMembers()
    {
        var values = Enum.GetValues<SessionStatus>();

        Assert.Equal(4, values.Length);
        Assert.Contains(SessionStatus.Scheduled, values);
        Assert.Contains(SessionStatus.Completed, values);
        Assert.Contains(SessionStatus.Cancelled, values);
        Assert.Contains(SessionStatus.NoShow, values);
    }
}
