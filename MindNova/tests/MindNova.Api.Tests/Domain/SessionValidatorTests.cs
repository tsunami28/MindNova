using MindNova.Domain.Entities;
using MindNova.Domain.Validation;

namespace MindNova.Api.Tests.Domain;

public class SessionValidatorTests
{
    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-10")]
    public void Validate_EmptyClientId_ReturnsError()
    {
        var session = CreateValidSession();
        session.ClientId = Guid.Empty;

        var errors = SessionValidator.Validate(session);

        Assert.Contains(errors, e => e.Contains("ClientId"));
    }

    [Theory]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-11")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceTherapistUserId_ReturnsError(string therapistUserId)
    {
        var session = CreateValidSession();
        session.TherapistUserId = therapistUserId;

        var errors = SessionValidator.Validate(session);

        Assert.Contains(errors, e => e.Contains("TherapistUserId"));
    }

    [Theory]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-12")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public void Validate_NonPositiveDurationMinutes_ReturnsError(int duration)
    {
        var session = CreateValidSession();
        session.DurationMinutes = duration;

        var errors = SessionValidator.Validate(session);

        Assert.Contains(errors, e => e.Contains("DurationMinutes"));
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-10")]
    public void Validate_ValidSession_ReturnsNoErrors()
    {
        var session = CreateValidSession();

        var errors = SessionValidator.Validate(session);

        Assert.Empty(errors);
    }

    private static Session CreateValidSession()
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            TherapistUserId = "therapist-user-id-123",
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 50,
            SessionType = SessionType.Individual,
            Status = SessionStatus.Scheduled,
            Notes = "Initial consultation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
