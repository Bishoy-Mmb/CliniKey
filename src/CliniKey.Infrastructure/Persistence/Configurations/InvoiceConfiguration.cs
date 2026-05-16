using CliniKey.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKey.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(i => i.TreatmentPlanId)
            .HasColumnName("treatment_plan_id");

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.OwnsMany(i => i.Lines, lineBuilder =>
        {
            lineBuilder.ToTable("invoice_lines");

            lineBuilder.HasKey(l => l.Id);

            lineBuilder.Property(l => l.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            lineBuilder.Property(l => l.InvoiceId)
                .HasColumnName("invoice_id")
                .IsRequired();

            lineBuilder.WithOwner().HasForeignKey(l => l.InvoiceId);

            lineBuilder.Property(l => l.Description)
                .HasColumnName("description")
                .HasMaxLength(300)
                .IsRequired();

            lineBuilder.Property(l => l.VatRate)
                .HasColumnName("vat_rate")
                .HasPrecision(18, 4)
                .IsRequired();

            lineBuilder.OwnsOne(l => l.Amount, amountBuilder =>
            {
                amountBuilder.Property(a => a.Amount)
                    .HasColumnName("amount_amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                amountBuilder.Property(a => a.Currency)
                    .HasColumnName("amount_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });

        builder.OwnsMany(i => i.Payments, paymentBuilder =>
        {
            paymentBuilder.ToTable("payments");

            paymentBuilder.HasKey(p => p.Id);

            paymentBuilder.Property(p => p.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            paymentBuilder.Property(p => p.InvoiceId)
                .HasColumnName("invoice_id")
                .IsRequired();

            paymentBuilder.WithOwner().HasForeignKey(p => p.InvoiceId);

            paymentBuilder.Property(p => p.Method)
                .HasColumnName("method")
                .IsRequired();

            paymentBuilder.Property(p => p.PaidAtUtc)
                .HasColumnName("paid_at_utc")
                .IsRequired();

            paymentBuilder.Property(p => p.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(100);

            paymentBuilder.OwnsOne(p => p.Amount, amountBuilder =>
            {
                amountBuilder.Property(a => a.Amount)
                    .HasColumnName("amount_amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                amountBuilder.Property(a => a.Currency)
                    .HasColumnName("amount_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });

        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(i => i.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
