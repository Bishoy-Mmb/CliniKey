using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Commands.UpdateClinicContact;

internal sealed class UpdateClinicContactCommandHandler : ICommandHandler<UpdateClinicContactCommand>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClinicContactCommandHandler(IClinicRepository clinicRepository, IUnitOfWork unitOfWork)
    {
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateClinicContactCommand request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure(ClinicErrors.NotFound);
        }

        var phoneResult = PhoneNumber.Create(request.Phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure(phoneResult.Error);
        }

        if (clinic.Phone != phoneResult.Value
            && await _clinicRepository.ExistsByPhoneAsync(phoneResult.Value, clinic.Id, cancellationToken))
        {
            return Result.Failure(TenantErrors.DuplicatePhone);
        }

        var updateResult = clinic.UpdateContact(request.Phone, request.Address);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
