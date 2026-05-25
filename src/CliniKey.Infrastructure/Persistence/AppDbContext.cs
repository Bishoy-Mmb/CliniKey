using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace CliniKey.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly string _sharedSchema;

    internal string SharedSchema => _sharedSchema;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : this(options, Options.Create(new TenancyOptions()))
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<TenancyOptions> tenancyOptions)
        : base(options)
    {
        _sharedSchema = tenancyOptions.Value.SharedSchema;
    }

    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<Dentist> Dentists { get; set; } = null!;
    public DbSet<Clinic> Clinics { get; set; } = null!;
    public DbSet<ClinicDentist> ClinicDentists { get; set; } = null!;
    public DbSet<TenantProvisioningAuditLog> TenantProvisioningAuditLogs { get; set; } = null!;
    public DbSet<TreatmentPlan> TreatmentPlans { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenancyModelCacheKeyFactory>();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClinicConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.ClinicDentistConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.DentistConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.PatientConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TenantConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.TenantProvisioningAuditLogConfiguration(_sharedSchema));
        modelBuilder.ApplyConfiguration(new Configurations.TreatmentPlanConfiguration());

        modelBuilder.Entity<Tenant>().ToTable("tenants", _sharedSchema, table => table.ExcludeFromMigrations());
        modelBuilder.Entity<Clinic>().ToTable("clinics", _sharedSchema, table => table.ExcludeFromMigrations());
        modelBuilder.Entity<Dentist>().ToTable("dentists", _sharedSchema, table => table.ExcludeFromMigrations());
        modelBuilder.Entity<ClinicDentist>().ToTable("clinic_dentists", _sharedSchema, table => table.ExcludeFromMigrations());
        modelBuilder.Entity<TenantProvisioningAuditLog>().ToTable("tenant_provisioning_audit_logs", _sharedSchema, table => table.ExcludeFromMigrations());
        
        base.OnModelCreating(modelBuilder);
    }
}
