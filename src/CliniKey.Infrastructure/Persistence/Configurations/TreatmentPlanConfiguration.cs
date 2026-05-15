using CliniKey.Domain.Entities;
using CliniKey.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        builder.ToTable("treatment_plans");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(t => t.DentistId)
            .HasColumnName("dentist_id")
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // Computed method CalculateTotalEstimatedCost() is not mapped

        builder.OwnsMany(t => t.Items, ib =>
        {
            ib.ToTable("treatment_items");

            ib.HasKey(i => i.Id);
            
            ib.Property(i => i.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            ib.Property(i => i.TreatmentPlanId)
                .HasColumnName("treatment_plan_id")
                .IsRequired();

            ib.Property(i => i.Tooth)
                .HasColumnName("tooth_code")
                .HasConversion(
                    t => t.Value,
                    v => ToothCode.Create(v).Value)
                .IsRequired();

            ib.Property(i => i.ProcedureName)
                .HasColumnName("procedure_name")
                .HasMaxLength(200)
                .IsRequired();

            ib.Property(i => i.Status)
                .HasColumnName("status")
                .IsRequired();

            ib.OwnsOne(i => i.EstimatedCost, ec =>
            {
                ec.Property(m => m.Amount)
                    .HasColumnName("estimated_cost_amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                ec.Property(m => m.Currency)
                    .HasColumnName("estimated_cost_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
            
            ib.WithOwner().HasForeignKey(i => i.TreatmentPlanId);
        });
    }
}
