using Microsoft.Extensions.Diagnostics.HealthChecks;
using MindNova.Infrastructure;
using MindNova.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MindNova")
    ?? throw new InvalidOperationException("Connection string 'MindNova' not found.");

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MindNovaDbContext>("sqlserver");

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
