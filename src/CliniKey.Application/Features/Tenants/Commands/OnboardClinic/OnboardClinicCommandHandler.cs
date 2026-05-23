using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Commands.OnboardClinic;

internal sealed class OnboardClinicCommandHandler : ICommandHandler<OnboardClinicCommand, OnboardClinicResponse>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public OnboardClinicCommandHandler(
        IClinicRepository clinicRepository,
        ITenantProvisioningService tenantProvisioningService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _clinicRepository = clinicRepository;
        _tenantProvisioningService = tenantProvisioningService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<OnboardClinicResponse>> Handle(
        OnboardClinicCommand request,
        CancellationToken cancellationToken)
    {
        var phoneResult = PhoneNumber.Create(request.Phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure<OnboardClinicResponse>(phoneResult.Error);
        }

        if (await _clinicRepository.ExistsByPhoneAsync(phoneResult.Value, null, cancellationToken))
        {
            return Result.Failure<OnboardClinicResponse>(TenantErrors.DuplicatePhone);
        }

        var clinicId = Guid.NewGuid();
        var schemaName = GenerateSchemaName(clinicId);
        var clinicResult = Clinic.Create(clinicId, request.Name, request.Phone, request.Address, schemaName, _clock);
        if (clinicResult.IsFailure)
        {
            return Result.Failure<OnboardClinicResponse>(clinicResult.Error);
        }

        var clinic = clinicResult.Value;
        clinic.MarkProvisioning();
        _clinicRepository.Add(clinic);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Guid? operatorUserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
        var provisioningResult = await _tenantProvisioningService.ProvisionAsync(
            clinic,
            operatorUserId,
            cancellationToken);

        if (provisioningResult.IsFailure)
        {
            _clinicRepository.Remove(clinic);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<OnboardClinicResponse>(provisioningResult.Error);
        }

        var provisionedResult = clinic.MarkProvisioned(provisioningResult.Value);
        if (provisionedResult.IsFailure)
        {
            return Result.Failure<OnboardClinicResponse>(provisionedResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OnboardClinicResponse.FromClinic(clinic);
    }

    private static string GenerateSchemaName(Guid clinicId)
    {
        return $"tenant_{clinicId:N}"[..15];
    }
}
