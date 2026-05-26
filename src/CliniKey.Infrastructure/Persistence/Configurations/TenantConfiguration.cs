using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    private readonly string _sharedSchema;

    public TenantConfiguration(string sharedSchema = TenancyOptions.DefaultSharedSchema)
    {
        _sharedSchema = sharedSchema;
    }

    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", _sharedSchema);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(Tenant.MaxNameLength);

        builder.Property(t => t.SchemaName)
            .HasColumnName("schema_name")
            .IsRequired()
            .HasMaxLength(Tenant.MaxSchemaNameLength);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.ProvisioningStatus)
            .HasColumnName("provisioning_status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.SchemaHealthStatus)
            .HasColumnName("schema_health_status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.CurrentMigration)
            .HasColumnName("current_migration")
            .HasMaxLength(Tenant.MaxMigrationLength);

        builder.Property(t => t.LastSchemaVerifiedAtUtc)
            .HasColumnName("last_schema_verified_at_utc");

        builder.Property(t => t.DeactivatedAtUtc)
            .HasColumnName("deactivated_at_utc");

        builder.Property(t => t.DeactivatedByUserId)
            .HasColumnName("deactivated_by_user_id");

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(t => t.SchemaName).IsUnique();

        builder.HasMany(t => t.Clinics)
            .WithOne()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
