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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
    }
}
