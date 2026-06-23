using MindNova.Domain.Validation;

namespace MindNova.Api.Tests.Domain;

public class ClientValidatorTests
{
    [Theory]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-7")]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_MissingFirstName_ReturnsError(string firstName)
    {
        var client = CreateValidClient();
        client.FirstName = firstName;

        var errors = ClientValidator.Validate(client);

        Assert.Contains(errors, e => e.Contains("FirstName"));
    }

    [Theory]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-7")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingLastName_ReturnsError(string lastName)
    {
        var client = CreateValidClient();
        client.LastName = lastName;

        var errors = ClientValidator.Validate(client);

        Assert.Contains(errors, e => e.Contains("LastName"));
    }

    [Theory]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-7")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEmail_ReturnsError(string email)
    {
        var client = CreateValidClient();
        client.Email = email;

        var errors = ClientValidator.Validate(client);

        Assert.Contains(errors, e => e.Contains("Email"));
    }

    [Theory]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-8")]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@no-local-part.com")]
    [InlineData("spaces in@email.com")]
    public void Validate_InvalidEmailFormat_ReturnsError(string email)
    {
        var client = CreateValidClient();
        client.Email = email;

        var errors = ClientValidator.Validate(client);

        Assert.Contains(errors, e => e.Contains("Email") && e.Contains("format"));
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-7")]
    public void Validate_ValidClient_ReturnsNoErrors()
    {
        var client = CreateValidClient();

        var errors = ClientValidator.Validate(client);

        Assert.Empty(errors);
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-7")]
    public void Validate_AllRequiredFieldsMissing_ReturnsMultipleErrors()
    {
        var client = new MindNova.Domain.Entities.Client();

        var errors = ClientValidator.Validate(client);

        Assert.True(errors.Count >= 3);
    }

    private static MindNova.Domain.Entities.Client CreateValidClient()
    {
        return new MindNova.Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DateOfBirth = new DateTime(1990, 1, 15),
            Phone = "+31612345678",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "+31698765432",
            Address = "123 Main Street",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
