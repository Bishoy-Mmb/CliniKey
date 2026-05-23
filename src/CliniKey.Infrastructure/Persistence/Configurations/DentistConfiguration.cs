using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class DentistConfiguration : IEntityTypeConfiguration<Dentist>
{
    public void Configure(EntityTypeBuilder<Dentist> builder)
    {
        builder.ToTable("dentists");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(d => d.FullName)
            .HasColumnName("full_name")
            .IsRequired()
            .HasMaxLength(Dentist.MaxFullNameLength);

        builder.Property(d => d.Specialization)
            .HasColumnName("specialization")
            .IsRequired()
            .HasMaxLength(Dentist.MaxSpecializationLength);

        builder.Property(d => d.LicenseNumber)
            .HasColumnName("license_number")
            .IsRequired()
            .HasMaxLength(Dentist.MaxLicenseNumberLength);

        builder.Property(d => d.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(d => d.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(d => d.LicenseNumber).IsUnique();
    }
}
