using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Auth;

public class SqlServerFixture : IAsyncLifetime
{
    private const string SaPassword = "Strong_password_123!";
    private const int ContainerPort = 1433;

    private IContainer _container;
    public WebApplicationFactory<Program> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    public string GetConnectionString()
    {
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(ContainerPort);
        return $"Server={host},{port};Database=MindNova;User Id=sa;Password={SaPassword};TrustServerCertificate=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithPortBinding(ContainerPort, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .AddCustomWaitStrategy(new WaitUntilSqlReady(this)))
            .Build();

        await _container.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MindNovaDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<MindNovaDbContext>(options =>
                        options.UseSqlServer(GetConnectionString()));
                });
            });

        // Apply migrations before creating the client (which triggers Program.cs startup and RoleSeeder)
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindNovaDbContext>();
            await db.Database.MigrateAsync();
        }

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (Client != null) Client.Dispose();
        if (Factory != null) await Factory.DisposeAsync();
        if (_container != null) await _container.DisposeAsync();
    }

    private class WaitUntilSqlReady : IWaitUntil
    {
        private readonly SqlServerFixture _owner;
        public WaitUntilSqlReady(SqlServerFixture owner) => _owner = owner;

        public async Task<bool> UntilAsync(IContainer container)
        {
            try
            {
                var host = container.Hostname;
                var port = container.GetMappedPublicPort(ContainerPort);
                var cs = $"Server={host},{port};Database=master;User Id=sa;Password={SaPassword};TrustServerCertificate=true;Connect Timeout=5";
                using var connection = new SqlConnection(cs);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
