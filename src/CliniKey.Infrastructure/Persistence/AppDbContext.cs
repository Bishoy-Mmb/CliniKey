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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}
