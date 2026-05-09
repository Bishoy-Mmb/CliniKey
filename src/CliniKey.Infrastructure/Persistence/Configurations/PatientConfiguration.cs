using CliniKey.Domain.Entities;
using CliniKey.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.OwnsOne(p => p.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            nameBuilder.Property(n => n.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(p => p.Phone)
            .HasColumnName("phone")
            .HasMaxLength(11)
            .HasConversion(
                phone => phone.Value,
                value => PhoneNumber.Create(value).Value)
            .IsRequired();

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth")
            .IsRequired();

        builder.Property(p => p.Gender)
            .HasColumnName("gender")
            .IsRequired();

        builder.Property(p => p.InsuranceDetails)
            .HasColumnName("insurance_details")
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.HasIndex(p => p.Phone).IsUnique();

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
