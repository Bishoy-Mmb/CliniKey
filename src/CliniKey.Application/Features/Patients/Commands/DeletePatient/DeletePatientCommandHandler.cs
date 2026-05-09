using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Patients.Commands.DeletePatient;

internal sealed class DeletePatientCommandHandler : ICommandHandler<DeletePatientCommand>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePatientCommandHandler(IPatientRepository patientRepository, IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);

        if (patient is null)
        {
            return Result.Failure(PatientErrors.NotFound(request.PatientId));
        }

        patient.SoftDelete();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
