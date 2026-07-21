using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TherapistProfileId)
            .IsRequired();

        builder.Property(a => a.StartTime)
            .IsRequired();

        builder.Property(a => a.EndTime)
            .IsRequired();

        builder.HasOne<TherapistProfile>()
            .WithMany()
            .HasForeignKey(a => a.TherapistProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.TherapistProfileId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilitySlot_DayOrDate",
            "(IsRecurring = 1 AND DayOfWeek IS NOT NULL AND SpecificDate IS NULL) OR (IsRecurring = 0 AND DayOfWeek IS NULL AND SpecificDate IS NOT NULL)"));
    }
}
