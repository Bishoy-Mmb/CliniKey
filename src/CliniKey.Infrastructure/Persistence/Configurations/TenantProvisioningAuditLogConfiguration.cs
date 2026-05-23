using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class TenantProvisioningAuditLogConfiguration : IEntityTypeConfiguration<TenantProvisioningAuditLog>
{
    public void Configure(EntityTypeBuilder<TenantProvisioningAuditLog> builder)
    {
        builder.ToTable("tenant_provisioning_audit_logs", "shared");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(log => log.ClinicId)
            .HasColumnName("clinic_id");

        builder.Property(log => log.SchemaName)
            .HasColumnName("schema_name")
            .HasMaxLength(TenantProvisioningAuditLog.MaxSchemaNameLength);

        builder.Property(log => log.Operation)
            .HasColumnName("operation")
            .IsRequired()
            .HasMaxLength(TenantProvisioningAuditLog.MaxOperationLength);

        builder.Property(log => log.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(TenantProvisioningAuditLog.MaxStatusLength);

        builder.Property(log => log.Message)
            .HasColumnName("message")
            .HasMaxLength(TenantProvisioningAuditLog.MaxMessageLength);

        builder.Property(log => log.OperatorUserId)
            .HasColumnName("operator_user_id");

        builder.Property(log => log.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.HasIndex(log => new { log.ClinicId, log.Operation });

        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(log => log.ClinicId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
