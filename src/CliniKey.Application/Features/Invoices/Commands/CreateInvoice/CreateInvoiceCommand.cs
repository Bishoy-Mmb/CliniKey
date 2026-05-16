using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Invoices.Commands.CreateInvoice;

public sealed record CreateInvoiceCommand(Guid TreatmentPlanId) : ICommand<Guid>;
