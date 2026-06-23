using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class MindNovaDbContext : IdentityDbContext<ApplicationUser>
{
    public MindNovaDbContext(DbContextOptions<MindNovaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<AvailabilityBlock> AvailabilityBlocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new AvailabilityBlockConfiguration());
    }
}
