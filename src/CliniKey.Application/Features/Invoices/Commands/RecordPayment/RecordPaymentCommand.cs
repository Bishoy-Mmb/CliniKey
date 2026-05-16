using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Enums;

namespace CliniKey.Application.Features.Invoices.Commands.RecordPayment;

public sealed record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string? ReferenceNumber) : ICommand<Guid>;
