using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class ClientConfigurationTests
{
    private readonly MindNovaDbContext _context;

    public ClientConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer("Server=localhost;Database=fake;TrustServerCertificate=true")
            .Options;

        _context = new MindNovaDbContext(options);
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-2")]
    public void DbContext_HasDbSetForClient()
    {
        var dbSet = _context.Set<Client>();

        Assert.NotNull(dbSet);
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-2")]
    public void DbContext_ClientsProperty_IsAccessible()
    {
        Assert.NotNull(_context.Clients);
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_FirstName_IsRequired_WithMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.FirstName));

        Assert.False(property.IsNullable);
        Assert.Equal(100, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_LastName_IsRequired_WithMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.LastName));

        Assert.False(property.IsNullable);
        Assert.Equal(100, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_Email_IsRequired_WithMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.Email));

        Assert.False(property.IsNullable);
        Assert.Equal(200, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_Phone_HasMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.Phone));

        Assert.Equal(30, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_EmergencyContactName_HasMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.EmergencyContactName));

        Assert.Equal(200, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_EmergencyContactPhone_HasMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.EmergencyContactPhone));

        Assert.Equal(30, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-3")]
    public void ClientConfiguration_Address_HasMaxLength()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var property = entityType.FindProperty(nameof(Client.Address));

        Assert.Equal(500, property.GetMaxLength());
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-4")]
    public void ClientConfiguration_HasIndex_OnLastName()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var lastNameProperty = entityType.FindProperty(nameof(Client.LastName));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Client.LastName)));
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-4")]
    public void ClientConfiguration_HasIndex_OnEmail()
    {
        var entityType = _context.Model.FindEntityType(typeof(Client));
        var indexes = entityType.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Client.Email)));
    }
}
