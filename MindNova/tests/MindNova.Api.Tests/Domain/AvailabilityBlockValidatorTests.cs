using MindNova.Domain.Entities;
using MindNova.Domain.Validation;

namespace MindNova.Api.Tests.Domain;

public class AvailabilityBlockValidatorTests
{
    [Theory]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-8")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyTherapistUserId_ReturnsError(string therapistId)
    {
        var block = CreateValidRecurringBlock();
        block.TherapistUserId = therapistId;

        var errors = AvailabilityBlockValidator.Validate(block);

        Assert.Contains(errors, e => e.Contains("TherapistUserId"));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-9")]
    public void Validate_StartTimeEqualsEndTime_ReturnsError()
    {
        var block = CreateValidRecurringBlock();
        block.StartTime = new TimeSpan(9, 0, 0);
        block.EndTime = new TimeSpan(9, 0, 0);

        var errors = AvailabilityBlockValidator.Validate(block);

        Assert.Contains(errors, e => e.Contains("StartTime"));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-9")]
    public void Validate_StartTimeAfterEndTime_ReturnsError()
    {
        var block = CreateValidRecurringBlock();
        block.StartTime = new TimeSpan(17, 0, 0);
        block.EndTime = new TimeSpan(9, 0, 0);

        var errors = AvailabilityBlockValidator.Validate(block);

        Assert.Contains(errors, e => e.Contains("StartTime"));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-10")]
    public void Validate_RecurringWithoutDayOfWeek_ReturnsError()
    {
        var block = CreateValidRecurringBlock();
        block.DayOfWeek = null;

        var errors = AvailabilityBlockValidator.Validate(block);

        Assert.Contains(errors, e => e.Contains("DayOfWeek"));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-11")]
    public void Validate_ValidRecurringBlock_ReturnsNoErrors()
    {
        var block = CreateValidRecurringBlock();

        var errors = AvailabilityBlockValidator.Validate(block);

        Assert.Empty(errors);
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-12")]
    public void Validate_ValidOneOffBlock_ReturnsNoErrors()
    {
        var block = new AvailabilityBlock
        {
            Id = Guid.NewGuid(),
            TherapistUserId = "therapist-123",
            DayOfWeek = null,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(14, 0, 0),
            EffectiveFrom = DateTime.UtcNow,
            IsRecurring = false,
            SpecificDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var errors = AvailabilityBlockValidator.Validate(block);

        Assert.Empty(errors);
    }

    private static AvailabilityBlock CreateValidRecurringBlock()
    {
        return new AvailabilityBlock
        {
            Id = Guid.NewGuid(),
            TherapistUserId = "therapist-123",
            DayOfWeek = System.DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            EffectiveFrom = DateTime.UtcNow,
            IsRecurring = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
