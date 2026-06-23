using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class AvailabilityBlockConfigurationTests
{
    private readonly MindNovaDbContext _context;

    public AvailabilityBlockConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer("Server=localhost;Database=fake;TrustServerCertificate=true")
            .Options;

        _context = new MindNovaDbContext(options);
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-2")]
    public void DbContext_HasDbSetForAvailabilityBlock()
    {
        Assert.NotNull(_context.AvailabilityBlocks);
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-3")]
    public void Configuration_HasForeignKey_ToApplicationUser()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilityBlock));
        var fks = entityType.GetForeignKeys().ToList();

        Assert.Contains(fks, fk => fk.Properties.Any(p => p.Name == nameof(AvailabilityBlock.TherapistUserId)));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-3")]
    public void Configuration_TherapistUserId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilityBlock));
        var property = entityType.FindProperty(nameof(AvailabilityBlock.TherapistUserId));

        Assert.False(property.IsNullable);
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-4")]
    public void Configuration_HasIndex_OnTherapistUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilityBlock));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(AvailabilityBlock.TherapistUserId)));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-4")]
    public void Configuration_HasIndex_OnDayOfWeek()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilityBlock));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(AvailabilityBlock.DayOfWeek)));
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-5")]
    public void Configuration_StartTime_MapsToTimeSqlType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilityBlock));
        var property = entityType.FindProperty(nameof(AvailabilityBlock.StartTime));

        Assert.Equal("time(7)", property.GetColumnType());
    }

    [Fact]
    [Trait("Story", "MN-20")]
    [Trait("AC", "AC-5")]
    public void Configuration_EndTime_MapsToTimeSqlType()
    {
        var entityType = _context.Model.FindEntityType(typeof(AvailabilityBlock));
        var property = entityType.FindProperty(nameof(AvailabilityBlock.EndTime));

        Assert.Equal("time(7)", property.GetColumnType());
    }
}
