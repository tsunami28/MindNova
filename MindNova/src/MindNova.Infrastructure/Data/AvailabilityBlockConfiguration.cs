using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class AvailabilityBlockConfiguration : IEntityTypeConfiguration<AvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<AvailabilityBlock> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TherapistUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.StartTime)
            .HasColumnType("time(7)");

        builder.Property(a => a.EndTime)
            .HasColumnType("time(7)");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.TherapistUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TherapistUserId);
        builder.HasIndex(a => a.DayOfWeek);
    }
}
