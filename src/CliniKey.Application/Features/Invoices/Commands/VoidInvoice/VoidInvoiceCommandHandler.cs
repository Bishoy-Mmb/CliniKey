using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Invoices.Commands.VoidInvoice;

internal sealed class VoidInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<VoidInvoiceCommand>
{
    public async Task<Result> Handle(VoidInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure(Domain.Errors.InvoiceErrors.NotFound(request.InvoiceId));
        }

        var voidResult = invoice.Void();
        if (voidResult.IsFailure)
        {
            return Result.Failure(voidResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
