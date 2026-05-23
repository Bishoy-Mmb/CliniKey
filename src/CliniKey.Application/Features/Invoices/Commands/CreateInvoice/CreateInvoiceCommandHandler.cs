using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Invoices.Commands.CreateInvoice;

internal sealed class CreateInvoiceCommandHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdAsync(request.TreatmentPlanId, cancellationToken);
        if (treatmentPlan is null)
        {
            return Result.Failure<Guid>(Domain.Errors.TreatmentPlanErrors.NotFound(request.TreatmentPlanId));
        }

        var invoiceResult = Invoice.CreateFromTreatmentPlan(treatmentPlan, clock);
        if (invoiceResult.IsFailure)
        {
            return Result.Failure<Guid>(invoiceResult.Error);
        }

        invoiceRepository.Add(invoiceResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return invoiceResult.Value.Id;
    }
}
