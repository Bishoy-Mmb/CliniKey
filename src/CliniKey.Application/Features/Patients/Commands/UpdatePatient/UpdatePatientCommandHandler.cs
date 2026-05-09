using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Patients.Commands.UpdatePatient;

internal sealed class UpdatePatientCommandHandler : ICommandHandler<UpdatePatientCommand>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePatientCommandHandler(IPatientRepository patientRepository, IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);

        if (patient is null)
        {
            return Result.Failure(PatientErrors.NotFound(request.PatientId));
        }

        var phoneResult = PhoneNumber.Create(request.PhoneNumber);

        if (phoneResult.IsFailure)
        {
            return Result.Failure(phoneResult.Error);
        }

        if (patient.Phone != phoneResult.Value)
        {
            if (await _patientRepository.ExistsByPhoneAsync(phoneResult.Value, cancellationToken))
            {
                return Result.Failure(PatientErrors.DuplicatePhone);
            }

            patient.UpdatePhone(phoneResult.Value);
        }

        patient.UpdateInsurance(request.InsuranceDetails);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
