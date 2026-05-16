using System.Data;
using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Dapper;

namespace CliniKey.Application.Features.Invoices.Queries.GetInvoiceById;

internal sealed class GetInvoiceByIdQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetInvoiceByIdQuery, InvoiceResponse>
{
    private sealed record InvoiceRow(
        Guid Id, Guid PatientId, Guid? TreatmentPlanId, int Status);

    private sealed record InvoiceLineRow(
        Guid? LineId, string? Description, decimal? Amount, decimal? VatRate, string? Currency);

    private sealed record PaymentRow(
        Guid? PaymentId, decimal? PaymentAmount, string? PaymentCurrency, int? Method, DateTime? PaidAtUtc, string? ReferenceNumber);

    public async Task<Result<InvoiceResponse>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                i.id AS Id, 
                i.patient_id AS PatientId, 
                i.treatment_plan_id AS TreatmentPlanId, 
                i.status AS Status,
                l.id AS LineId,
                l.description AS Description,
                l.amount_amount AS Amount,
                l.amount_currency AS Currency,
                l.vat_rate AS VatRate,
                p.id AS PaymentId,
                p.amount_amount AS PaymentAmount,
                p.amount_currency AS PaymentCurrency,
                p.method AS Method,
                p.paid_at_utc AS PaidAtUtc,
                p.reference_number AS ReferenceNumber
            FROM invoices i
            LEFT JOIN invoice_lines l ON i.id = l.invoice_id
            LEFT JOIN payments p ON i.id = p.invoice_id
            WHERE i.id = @InvoiceId
            """;

        var lookup = new Dictionary<Guid, InvoiceResponse>();

        await connection.QueryAsync<InvoiceRow, InvoiceLineRow, PaymentRow, InvoiceResponse>(
            sql,
            (invoice, line, payment) =>
            {
                if (!lookup.TryGetValue(invoice.Id, out InvoiceResponse? response))
                {
                    response = new InvoiceResponse(
                        invoice.Id,
                        invoice.PatientId,
                        invoice.TreatmentPlanId,
                        ((InvoiceStatus)invoice.Status).ToString(),
                        0m, 0m, 0m, "EGP",
                        new List<InvoiceLineResponse>(),
                        new List<PaymentResponse>()
                    );
                    lookup.Add(invoice.Id, response);
                }

                if (line?.LineId is not null && !response.Lines.Any(x => x.Id == line.LineId.Value))
                {
                    var vatAmount = line.Amount!.Value * line.VatRate!.Value;
                    response.Lines.Add(new InvoiceLineResponse(
                        line.LineId.Value,
                        line.Description!,
                        line.Amount!.Value,
                        line.VatRate!.Value,
                        vatAmount,
                        line.Currency!
                    ));
                }

                if (payment?.PaymentId is not null && !response.Payments.Any(x => x.Id == payment.PaymentId.Value))
                {
                    response.Payments.Add(new PaymentResponse(
                        payment.PaymentId.Value,
                        payment.PaymentAmount!.Value,
                        payment.PaymentCurrency!,
                        ((PaymentMethod)payment.Method!.Value).ToString(),
                        payment.PaidAtUtc!.Value,
                        payment.ReferenceNumber
                    ));
                }

                return response;
            },
            new { request.InvoiceId },
            splitOn: "LineId,PaymentId"
        );

        if (!lookup.TryGetValue(request.InvoiceId, out InvoiceResponse? result))
        {
            return Result.Failure<InvoiceResponse>(InvoiceErrors.NotFound(request.InvoiceId));
        }

        var subtotal = result.Lines.Sum(l => l.Amount);
        var vatAmount = result.Lines.Sum(l => l.VatAmount);
        var totalAmount = subtotal + vatAmount;
        var currency = result.Lines.FirstOrDefault()?.Currency ?? "EGP";

        var finalResponse = result with 
        { 
            SubtotalAmount = subtotal,
            VatAmount = vatAmount,
            TotalAmount = totalAmount,
            Currency = currency 
        };

        return finalResponse;
    }
}
