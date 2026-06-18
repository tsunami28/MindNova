using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Infrastructure;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class DbContextRegistrationTests
{
    [Fact]
    [Trait("Story", "MN-9")]
    [Trait("AC", "AC-5")]
    public void DbContext_ResolvesFromServiceProvider_WithSqlServerProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Server=localhost;Database=test;TrustServerCertificate=true");

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<MindNovaDbContext>();

        Assert.NotNull(context);
        Assert.True(context.Database.IsSqlServer());
    }
}
