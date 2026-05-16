namespace CliniKey.Application.DTOs;

public sealed record InvoiceResponse(
    Guid Id,
    Guid PatientId,
    Guid? TreatmentPlanId,
    string Status,
    decimal SubtotalAmount,
    decimal VatAmount,
    decimal TotalAmount,
    string Currency,
    List<InvoiceLineResponse> Lines,
    List<PaymentResponse> Payments);

public sealed record InvoiceLineResponse(
    Guid Id,
    string Description,
    decimal Amount,
    decimal VatRate,
    decimal VatAmount,
    string Currency);

public sealed record PaymentResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string Method,
    DateTime PaidAtUtc,
    string? ReferenceNumber);
