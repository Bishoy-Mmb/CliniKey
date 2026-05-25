using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace CliniKey.Infrastructure.Persistence;

public sealed class SharedDbContext : DbContext
{
    private readonly string _sharedSchema;
    private readonly string _tenantSchemaPrefix;

    internal string SharedSchema => _sharedSchema;
    internal string TenantSchemaPrefix => _tenantSchemaPrefix;

    public SharedDbContext(DbContextOptions<SharedDbContext> options)
        : this(options, Options.Create(new TenancyOptions()))
    {
    }

    public SharedDbContext(DbContextOptions<SharedDbContext> options, IOptions<TenancyOptions> tenancyOptions)
        : base(options)
    {
        _sharedSchema = tenancyOptions.Value.SharedSchema;
        _tenantSchemaPrefix = tenancyOptions.Value.TenantSchemaPrefix;
    }

    public DbSet<Clinic> Clinics { get; set; } = null!;
    public DbSet<Dentist> Dentists { get; set; } = null!;
    public DbSet<ClinicDentist> ClinicDentists { get; set; } = null!;
    public DbSet<TenantProvisioningAuditLog> TenantProvisioningAuditLogs { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenancyModelCacheKeyFactory>();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.ClinicConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.DentistConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.ClinicDentistConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.TenantProvisioningAuditLogConfiguration(_sharedSchema));

        var clinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dentistId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<Clinic>().HasData(new
        {
            Id = clinicId,
            Name = "Dev Clinic",
            Phone = PhoneNumber.Create("01000000000").Value,
            Address = "Development tenant",
            SchemaName = $"{_tenantSchemaPrefix}dev",
            Status = ClinicStatus.Active,
            ProvisioningStatus = TenantProvisioningStatus.Provisioned,
            SchemaHealthStatus = TenantSchemaHealthStatus.Healthy,
            CurrentMigration = "SeededDevelopmentTenant",
            LastSchemaVerifiedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DeactivatedAtUtc = (DateTime?)null,
            DeactivatedByUserId = (Guid?)null,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<Dentist>().HasData(new
        {
            Id = dentistId,
            FullName = "Dr. Dev",
            Specialization = "General Dentistry",
            LicenseNumber = "LIC-DEV-001",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<ClinicDentist>().HasData(new
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ClinicId = clinicId,
            DentistId = dentistId
        });

        base.OnModelCreating(modelBuilder);
    }
}
