using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    private readonly string _sharedSchema;

    public ClinicConfiguration(string sharedSchema = TenancyOptions.DefaultSharedSchema)
    {
        _sharedSchema = sharedSchema;
    }

    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("clinics", _sharedSchema);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(Clinic.MaxNameLength);

        builder.Property(c => c.Phone)
            .HasColumnName("phone")
            .HasConversion(phone => phone.Value, value => CliniKey.Domain.ValueObjects.PhoneNumber.Create(value).Value)
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(c => c.Address)
            .HasColumnName("address")
            .IsRequired()
            .HasMaxLength(Clinic.MaxAddressLength);

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.DeactivatedAtUtc)
            .HasColumnName("deactivated_at_utc");

        builder.Property(c => c.DeactivatedByUserId)
            .HasColumnName("deactivated_by_user_id");

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Phone).IsUnique();
    }
}
