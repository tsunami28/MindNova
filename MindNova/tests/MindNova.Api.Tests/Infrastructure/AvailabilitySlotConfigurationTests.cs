using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class AvailabilitySlotConfigurationTests
{
    private readonly MindNovaDbContext _context;

    public AvailabilitySlotConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer("Server=localhost;Database=fake;TrustServerCertificate=true")
            .Options;

        _context = new MindNovaDbContext(options);
    }

    [Fact]
    [Trait("Story", "MN-28")]
    [Trait("AC", "AC-1")]
    public void AvailabilitySlot_HasExpectedProperties()
    {
        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            TherapistProfileId = Guid.NewGuid(),
            DayOfWeek = 1,
            SpecificDate = null,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsRecurring = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, slot.Id);
        Assert.NotEqual(Guid.Empty, slot.TherapistProfileId);
        Assert.Equal(1, slot.DayOfWeek);
        Assert.Null(slot.SpecificDate);
        Assert.Equal(new TimeSpan(9, 0, 0), slot.StartTime);
        Assert.Equal(new TimeSpan(12, 0, 0), slot.EndTime);
        Assert.True(slot.IsRecurring);
        Assert.NotEqual(default, slot.CreatedAt);
        Assert.NotEqual(default, slot.UpdatedAt);
    }

    [Fact]
    [Trait("Story", "MN-28")]
    [Trait("AC", "AC-2")]
    public void AvailabilitySlotConfiguration_HasForeignKey_ToTherapistProfile()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilitySlot));
        var fks = entityType.GetForeignKeys().ToList();

        Assert.Contains(fks, fk => fk.Properties.Any(p => p.Name == nameof(AvailabilitySlot.TherapistProfileId)));
    }

    [Fact]
    [Trait("Story", "MN-28")]
    [Trait("AC", "AC-2")]
    public void AvailabilitySlotConfiguration_HasIndex_OnTherapistProfileId()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilitySlot));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(AvailabilitySlot.TherapistProfileId)));
    }

    [Fact]
    [Trait("Story", "MN-28")]
    [Trait("AC", "AC-3")]
    public void AvailabilitySlot_RecurringSlot_RequiresDayOfWeek()
    {
        var recurring = new AvailabilitySlot
        {
            IsRecurring = true,
            DayOfWeek = 1,
            SpecificDate = null
        };

        Assert.True(recurring.IsRecurring);
        Assert.NotNull(recurring.DayOfWeek);
        Assert.Null(recurring.SpecificDate);
    }

    [Fact]
    [Trait("Story", "MN-28")]
    [Trait("AC", "AC-3")]
    public void AvailabilitySlot_OneOffSlot_RequiresSpecificDate()
    {
        var oneOff = new AvailabilitySlot
        {
            IsRecurring = false,
            DayOfWeek = null,
            SpecificDate = new DateTime(2026, 8, 1)
        };

        Assert.False(oneOff.IsRecurring);
        Assert.Null(oneOff.DayOfWeek);
        Assert.NotNull(oneOff.SpecificDate);
    }

    [Fact]
    [Trait("Story", "MN-28")]
    [Trait("AC", "AC-5")]
    public void DbContext_HasDbSetForAvailabilitySlot()
    {
        Assert.NotNull(_context.AvailabilitySlots);
    }
}
