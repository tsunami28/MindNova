using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;

namespace MindNova.Api.Tests;

public sealed class SqlServerContainer : IAsyncDisposable
{
    private const string SaPassword = "Strong_password_123!";
    private const int ContainerPort = 1433;

    private readonly IContainer _container;

    public SqlServerContainer()
    {
        _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithPortBinding(ContainerPort, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .AddCustomWaitStrategy(new SqlConnectionWaitStrategy(this)))
            .Build();
    }

    public string GetConnectionString()
    {
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(ContainerPort);
        return $"Server={host},{port};Database=master;User Id=sa;Password={SaPassword};TrustServerCertificate=true;Connect Timeout=5";
    }

    public async Task StartAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private sealed class SqlConnectionWaitStrategy : IWaitUntil
    {
        private readonly SqlServerContainer _owner;

        public SqlConnectionWaitStrategy(SqlServerContainer owner) => _owner = owner;

        public async Task<bool> UntilAsync(IContainer container)
        {
            try
            {
                await using var connection = new SqlConnection(_owner.GetConnectionString());
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
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
