using MindNova.Domain.Entities;

namespace MindNova.Api.Tests.Domain;

public class AvailabilityBlockEntityTests
{
    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-1")]
    public void AvailabilityBlock_HasAllElevenProperties_WithCorrectTypes()
    {
        var block = new AvailabilityBlock();

        Assert.IsType<Guid>(block.Id);
        Assert.IsType<string>(block.TherapistUserId);
        Assert.Null(block.DayOfWeek);
        Assert.IsType<TimeSpan>(block.StartTime);
        Assert.IsType<TimeSpan>(block.EndTime);
        Assert.IsType<DateTime>(block.EffectiveFrom);
        Assert.Null(block.EffectiveTo);
        Assert.IsType<bool>(block.IsRecurring);
        Assert.Null(block.SpecificDate);
        Assert.IsType<DateTime>(block.CreatedAt);
        Assert.IsType<DateTime>(block.UpdatedAt);
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-1")]
    public void AvailabilityBlock_DayOfWeek_AcceptsNullableEnum()
    {
        var block = new AvailabilityBlock { DayOfWeek = System.DayOfWeek.Monday };

        Assert.Equal(System.DayOfWeek.Monday, block.DayOfWeek);

        block.DayOfWeek = null;
        Assert.Null(block.DayOfWeek);
    }
}
