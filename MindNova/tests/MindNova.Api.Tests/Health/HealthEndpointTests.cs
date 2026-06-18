using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Health;

public class HealthEndpointTests : IAsyncLifetime
{
    private readonly SqlServerContainer _sqlContainer = new();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    [Trait("Story", "MN-9")]
    [Trait("AC", "AC-3")]
    public async Task Health_ReturnsOk_WhenDatabaseIsReachable()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MindNovaDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<MindNovaDbContext>(options =>
                        options.UseSqlServer(_sqlContainer.GetConnectionString()));
                });
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Story", "MN-9")]
    [Trait("AC", "AC-4")]
    public async Task Health_ReturnsServiceUnavailable_WhenDatabaseIsUnreachable()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MindNovaDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<MindNovaDbContext>(options =>
                        options.UseSqlServer("Server=invalid_host,1433;Database=MindNova;User Id=sa;Password=fake;TrustServerCertificate=true;Connect Timeout=1"));
                });
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
