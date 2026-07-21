using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class TherapistProfileConfiguration : IEntityTypeConfiguration<TherapistProfile>
{
    public void Configure(EntityTypeBuilder<TherapistProfile> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(t => t.Specialisations)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(t => t.MaxCaseload)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.UserId).IsUnique();
    }
}
