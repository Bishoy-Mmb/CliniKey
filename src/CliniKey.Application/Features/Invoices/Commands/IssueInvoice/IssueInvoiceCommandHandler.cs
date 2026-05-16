using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Invoices.Commands.IssueInvoice;

internal sealed class IssueInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<IssueInvoiceCommand>
{
    public async Task<Result> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure(Domain.Errors.InvoiceErrors.NotFound(request.InvoiceId));
        }

        var issueResult = invoice.Issue();
        if (issueResult.IsFailure)
        {
            return Result.Failure(issueResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
