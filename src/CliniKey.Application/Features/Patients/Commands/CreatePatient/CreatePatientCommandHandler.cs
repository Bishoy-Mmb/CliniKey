using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Patients.Commands.CreatePatient;

internal sealed class CreatePatientCommandHandler : ICommandHandler<CreatePatientCommand, Guid>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePatientCommandHandler(IPatientRepository patientRepository, IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var nameResult = PatientName.Create(request.FirstName, request.LastName);
        if (nameResult.IsFailure)
        {
            return Result.Failure<Guid>(nameResult.Error);
        }

        var phoneResult = PhoneNumber.Create(request.PhoneNumber);
        if (phoneResult.IsFailure)
        {
            return Result.Failure<Guid>(phoneResult.Error);
        }

        if (await _patientRepository.ExistsByPhoneAsync(phoneResult.Value, cancellationToken))
        {
            return Result.Failure<Guid>(PatientErrors.DuplicatePhone);
        }

        var patient = Patient.Create(
            nameResult.Value,
            phoneResult.Value,
            request.DateOfBirth,
            request.Gender,
            request.InsuranceDetails);

        _patientRepository.Add(patient);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return patient.Id;
    }
}
