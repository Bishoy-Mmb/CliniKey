using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Identity;

public sealed class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("public");

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(), 
            t => t.Namespace != null && t.Namespace.Contains("Identity.Configurations"));

        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000001"),
                Name = "ClinicAdmin",
                NormalizedName = "CLINICADMIN"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000002"),
                Name = "Dentist",
                NormalizedName = "DENTIST"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000003"),
                Name = "Receptionist",
                NormalizedName = "RECEPTIONIST"
            }
        );
    }
}
