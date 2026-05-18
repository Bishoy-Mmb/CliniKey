using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Identity.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");
        
        builder.Property(rt => rt.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        
        builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Property(rt => rt.FamilyId).HasColumnName("family_id").IsRequired();
        builder.HasIndex(rt => rt.FamilyId);
        
        builder.Property(rt => rt.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(rt => rt.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(rt => rt.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(rt => rt.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
    }
}
