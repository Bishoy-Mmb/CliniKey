using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Invoices.Commands.IssueInvoice;

public sealed record IssueInvoiceCommand(Guid InvoiceId) : ICommand;
