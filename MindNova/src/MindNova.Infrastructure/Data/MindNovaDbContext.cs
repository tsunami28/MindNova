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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
