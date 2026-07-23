using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class TreatmentNoteConfiguration : IEntityTypeConfiguration<TreatmentNote>
{
    public void Configure(EntityTypeBuilder<TreatmentNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.SessionId)
            .IsRequired();

        builder.Property(n => n.TherapistUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(n => n.PresentingIssue)
            .HasMaxLength(4000);

        builder.Property(n => n.Interventions)
            .HasMaxLength(4000);

        builder.Property(n => n.Homework)
            .HasMaxLength(2000);

        builder.Property(n => n.FreeText)
            .HasMaxLength(8000);

        builder.Property(n => n.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(n => n.DeletedByUserId)
            .HasMaxLength(450);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(n => n.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.SessionId);
        builder.HasIndex(n => n.TherapistUserId);
    }
}
