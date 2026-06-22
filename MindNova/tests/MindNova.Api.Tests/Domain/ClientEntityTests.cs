using MindNova.Domain.Entities;

namespace MindNova.Api.Tests.Domain;

public class ClientEntityTests
{
    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-1")]
    public void Client_HasAllTwelveProperties_WithCorrectTypes()
    {
        var client = new Client();

        Assert.IsType<Guid>(client.Id);
        Assert.IsType<string>(client.FirstName);
        Assert.IsType<string>(client.LastName);
        Assert.IsType<DateTime>(client.DateOfBirth);
        Assert.IsType<string>(client.Email);
        Assert.IsType<string>(client.Phone);
        Assert.IsType<string>(client.EmergencyContactName);
        Assert.IsType<string>(client.EmergencyContactPhone);
        Assert.IsType<string>(client.Address);
        Assert.IsType<DateTime>(client.CreatedAt);
        Assert.IsType<DateTime>(client.UpdatedAt);
        Assert.IsType<bool>(client.IsArchived);
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-1")]
    public void Client_DefaultValues_AreReasonable()
    {
        var client = new Client();

        Assert.Equal(Guid.Empty, client.Id);
        Assert.Equal(string.Empty, client.FirstName);
        Assert.Equal(string.Empty, client.LastName);
        Assert.Equal(string.Empty, client.Email);
        Assert.Equal(string.Empty, client.Phone);
        Assert.Equal(string.Empty, client.EmergencyContactName);
        Assert.Equal(string.Empty, client.EmergencyContactPhone);
        Assert.Equal(string.Empty, client.Address);
        Assert.False(client.IsArchived);
    }
}
