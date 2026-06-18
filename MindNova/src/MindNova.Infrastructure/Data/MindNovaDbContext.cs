using Microsoft.EntityFrameworkCore;

namespace MindNova.Infrastructure.Data;

public class MindNovaDbContext : DbContext
{
    public MindNovaDbContext(DbContextOptions<MindNovaDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
