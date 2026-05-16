using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Invoices.Commands.RecordPayment;

internal sealed class RecordPaymentCommandHandler(
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<Guid>(Domain.Errors.InvoiceErrors.NotFound(request.InvoiceId));
        }

        var amountResult = Money.Create(request.Amount, request.Currency);
        if (amountResult.IsFailure)
        {
            return Result.Failure<Guid>(amountResult.Error);
        }

        var paymentResult = invoice.RecordPayment(amountResult.Value, request.Method, request.ReferenceNumber);
        if (paymentResult.IsFailure)
        {
            return Result.Failure<Guid>(paymentResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
