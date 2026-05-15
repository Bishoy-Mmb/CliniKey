using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(a => a.DentistId)
            .HasColumnName("dentist_id")
            .IsRequired();

        builder.Property(a => a.StartTime)
            .HasColumnName("start_time")
            .IsRequired();

        builder.Property(a => a.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property<uint>("Version")
            .IsRowVersion()
            .HasColumnName("xmin");

        builder.HasIndex(a => new { a.DentistId, a.StartTime });
    }
}
