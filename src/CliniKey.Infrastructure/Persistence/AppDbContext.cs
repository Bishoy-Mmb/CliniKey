using CliniKey.Domain.Entities;
using CliniKey.SharedKernel.Primitives;
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
    public DbSet<TreatmentPlan> TreatmentPlans { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        var clinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dentistId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<Clinic>().HasData(new
        {
            Id = clinicId,
            Name = "Dev Clinic",
            SchemaName = "tenant_dev",
            IsActive = true,
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
