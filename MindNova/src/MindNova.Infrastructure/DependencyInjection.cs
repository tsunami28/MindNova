using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MindNova.Infrastructure.Data;

namespace MindNova.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MindNovaDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
