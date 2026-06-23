using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MindNova.Domain.Entities;

namespace MindNova.Infrastructure.Data;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.EmergencyContactName)
            .HasMaxLength(200);

        builder.Property(c => c.EmergencyContactPhone)
            .HasMaxLength(30);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.HasIndex(c => c.LastName);
        builder.HasIndex(c => c.Email);
    }
}
