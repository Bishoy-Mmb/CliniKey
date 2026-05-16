using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Invoices.Commands.VoidInvoice;

public sealed record VoidInvoiceCommand(Guid InvoiceId) : ICommand;
