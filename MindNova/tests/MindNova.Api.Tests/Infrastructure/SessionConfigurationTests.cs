using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class SessionConfigurationTests
{
    private readonly MindNovaDbContext _context;

    public SessionConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer("Server=localhost;Database=fake;TrustServerCertificate=true")
            .Options;

        _context = new MindNovaDbContext(options);
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-4")]
    public void DbContext_HasDbSetForSession()
    {
        Assert.NotNull(_context.Sessions);
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-5")]
    public void SessionConfiguration_HasForeignKey_ToClient()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var fks = entityType.GetForeignKeys().ToList();

        Assert.Contains(fks, fk => fk.Properties.Any(p => p.Name == nameof(Session.ClientId)));
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-5")]
    public void SessionConfiguration_HasForeignKey_ToApplicationUser()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var fks = entityType.GetForeignKeys().ToList();

        Assert.Contains(fks, fk => fk.Properties.Any(p => p.Name == nameof(Session.TherapistUserId)));
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-6")]
    public void SessionConfiguration_HasIndex_OnClientId()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Session.ClientId)));
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-6")]
    public void SessionConfiguration_HasIndex_OnTherapistUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Session.TherapistUserId)));
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-6")]
    public void SessionConfiguration_HasIndex_OnScheduledAt()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Session.ScheduledAt)));
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-7")]
    public void SessionConfiguration_ClientId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var property = entityType.FindProperty(nameof(Session.ClientId));

        Assert.False(property.IsNullable);
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-7")]
    public void SessionConfiguration_TherapistUserId_IsRequired_WithMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var property = entityType.FindProperty(nameof(Session.TherapistUserId));

        Assert.False(property.IsNullable);
        Assert.Equal(450, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-17")]
    [Trait("AC", "AC-7")]
    public void SessionConfiguration_Notes_HasMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Session));
        var property = entityType.FindProperty(nameof(Session.Notes));

        Assert.Equal(2000, property.GetMaxLength());
    }
}
