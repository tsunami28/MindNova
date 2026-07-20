using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class TherapistProfileConfigurationTests
{
    private readonly MindNovaDbContext _context;

    public TherapistProfileConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer("Server=localhost;Database=fake;TrustServerCertificate=true")
            .Options;

        _context = new MindNovaDbContext(options);
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-1")]
    public void TherapistProfile_HasExpectedProperties()
    {
        var profile = new TherapistProfile
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            Specialisations = new List<string> { "CBT", "EMDR" },
            MaxCaseload = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal("user-123", profile.UserId);
        Assert.Equal(2, profile.Specialisations.Count);
        Assert.Equal(10, profile.MaxCaseload);
        Assert.True(profile.IsActive);
        Assert.NotEqual(default, profile.CreatedAt);
        Assert.NotEqual(default, profile.UpdatedAt);
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-2")]
    public void TherapistProfileConfiguration_HasForeignKey_ToApplicationUser()
    {
        var entityType = _context.Model.FindEntityType(typeof(TherapistProfile));
        var fks = entityType.GetForeignKeys().ToList();

        Assert.Contains(fks, fk => fk.Properties.Any(p => p.Name == nameof(TherapistProfile.UserId)));
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-2")]
    public void TherapistProfileConfiguration_HasIndex_OnUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(TherapistProfile));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(TherapistProfile.UserId)));
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-3")]
    public void TherapistProfileConfiguration_Specialisations_HasNvarcharMaxColumnType()
    {
        var entityType = _context.Model.FindEntityType(typeof(TherapistProfile));
        var property = entityType.FindProperty(nameof(TherapistProfile.Specialisations));

        Assert.Equal("nvarchar(max)", property.GetColumnType());
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-3")]
    public void TherapistProfileConfiguration_Specialisations_HasValueConverter()
    {
        var entityType = _context.Model.FindEntityType(typeof(TherapistProfile));
        var property = entityType.FindProperty(nameof(TherapistProfile.Specialisations));

        Assert.NotNull(property.GetValueConverter());
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-5")]
    public void DbContext_HasDbSetForTherapistProfile()
    {
        Assert.NotNull(_context.TherapistProfiles);
    }

    [Fact]
    [Trait("Story", "MN-24")]
    [Trait("AC", "AC-6")]
    public void TherapistProfileConfiguration_UserId_HasUniqueIndex()
    {
        var entityType = _context.Model.FindEntityType(typeof(TherapistProfile));
        var indexes = entityType.GetIndexes();

        var userIdIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(TherapistProfile.UserId)));
        Assert.NotNull(userIdIndex);
        Assert.True(userIdIndex.IsUnique);
    }
}
