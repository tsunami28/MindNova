using Microsoft.EntityFrameworkCore;
using MindNova.Infrastructure.Data;

namespace MindNova.Api.Tests.Infrastructure;

public class MigrationTests : IAsyncLifetime
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
    [Trait("AC", "AC-6")]
    public async Task InitialMigration_AppliesCleanly_ToEmptyDatabase()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        await using var context = new MindNovaDbContext(options);

        await context.Database.MigrateAsync();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(appliedMigrations);
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-5")]
    public async Task AddClientsMigration_CreatesClientsTable()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        await using var context = new MindNovaDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Clients'";
        var result = await command.ExecuteScalarAsync();

        Assert.Equal(1, Convert.ToInt32(result));
    }

    [Fact]
    [Trait("Story", "MN-13")]
    [Trait("AC", "AC-6")]
    public async Task AddClientsMigration_AppliesCleanly_WithNoPendingMigrations()
    {
        var options = new DbContextOptionsBuilder<MindNovaDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        await using var context = new MindNovaDbContext(options);
        await context.Database.MigrateAsync();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);

        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains(appliedMigrations, m => m.Contains("AddClients"));
    }
}
