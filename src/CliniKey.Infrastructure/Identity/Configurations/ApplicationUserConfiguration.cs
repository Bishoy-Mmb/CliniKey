using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Identity.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.Property(u => u.DentistId).HasColumnName("dentist_id");
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.FullName).HasColumnName("full_name").HasMaxLength(200);
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}
