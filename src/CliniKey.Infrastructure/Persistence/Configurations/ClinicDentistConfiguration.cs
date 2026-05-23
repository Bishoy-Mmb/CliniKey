using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class ClinicDentistConfiguration : IEntityTypeConfiguration<ClinicDentist>
{
    public void Configure(EntityTypeBuilder<ClinicDentist> builder)
    {
        builder.ToTable("clinic_dentists", "shared");

        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(cd => cd.ClinicId)
            .HasColumnName("clinic_id")
            .IsRequired();

        builder.Property(cd => cd.DentistId)
            .HasColumnName("dentist_id")
            .IsRequired();

        builder.HasIndex(cd => new { cd.ClinicId, cd.DentistId }).IsUnique();

        builder.HasOne<Clinic>()
            .WithMany(c => c.ClinicDentists)
            .HasForeignKey(cd => cd.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Dentist>()
            .WithMany()
            .HasForeignKey(cd => cd.DentistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
