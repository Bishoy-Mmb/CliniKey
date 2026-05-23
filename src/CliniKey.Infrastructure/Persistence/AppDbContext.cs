using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Dentist> Dentists { get; set; } = null!;
    public DbSet<Clinic> Clinics { get; set; } = null!;
    public DbSet<ClinicDentist> ClinicDentists { get; set; } = null!;
    public DbSet<TenantProvisioningAuditLog> TenantProvisioningAuditLogs { get; set; } = null!;
    public DbSet<TreatmentPlan> TreatmentPlans { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClinicConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClinicDentistConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.DentistConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.PatientConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TenantProvisioningAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TreatmentPlanConfiguration());

        modelBuilder.Entity<Clinic>().ToTable("clinics", "shared", table => table.ExcludeFromMigrations());
        modelBuilder.Entity<Dentist>().ToTable("dentists", "shared", table => table.ExcludeFromMigrations());
        modelBuilder.Entity<ClinicDentist>().ToTable("clinic_dentists", "shared", table => table.ExcludeFromMigrations());
        modelBuilder.Entity<TenantProvisioningAuditLog>().ToTable("tenant_provisioning_audit_logs", "shared", table => table.ExcludeFromMigrations());
        
        base.OnModelCreating(modelBuilder);
    }
}
