using CliniKey.Domain.Enums;

namespace CliniKey.API.Contracts.Invoices;

public sealed record RecordPaymentRequest(decimal Amount, string Currency, PaymentMethod Method, string? ReferenceNumber);
