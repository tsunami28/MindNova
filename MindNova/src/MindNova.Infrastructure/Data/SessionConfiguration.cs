using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ClientId)
            .IsRequired();

        builder.Property(s => s.TherapistUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.ScheduledAt)
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.TherapistUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ClientId);
        builder.HasIndex(s => s.TherapistUserId);
        builder.HasIndex(s => s.ScheduledAt);
    }
}
