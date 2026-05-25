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
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantSchemaNameGenerator _tenantSchemaNameGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public OnboardClinicCommandHandler(
        IClinicRepository clinicRepository,
        ITenantRepository tenantRepository,
        ITenantProvisioningService tenantProvisioningService,
        ICurrentUserService currentUserService,
        ITenantSchemaNameGenerator tenantSchemaNameGenerator,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _clinicRepository = clinicRepository;
        _tenantRepository = tenantRepository;
        _tenantProvisioningService = tenantProvisioningService;
        _currentUserService = currentUserService;
        _tenantSchemaNameGenerator = tenantSchemaNameGenerator;
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

        var tenantId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var schemaName = _tenantSchemaNameGenerator.Generate(tenantId);
        var tenantResult = Tenant.Create(tenantId, request.Name, schemaName, _clock);
        if (tenantResult.IsFailure)
        {
            return Result.Failure<OnboardClinicResponse>(tenantResult.Error);
        }

        var clinicResult = Clinic.Create(clinicId, tenantId, request.Name, request.Phone, request.Address, _clock);
        if (clinicResult.IsFailure)
        {
            return Result.Failure<OnboardClinicResponse>(clinicResult.Error);
        }

        var tenant = tenantResult.Value;
        var clinic = clinicResult.Value;
        tenant.MarkProvisioning();
        _tenantRepository.Add(tenant);
        _clinicRepository.Add(clinic);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Guid? operatorUserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
        var provisioningResult = await _tenantProvisioningService.ProvisionAsync(
            tenant,
            operatorUserId,
            cancellationToken);

        if (provisioningResult.IsFailure)
        {
            // Best-effort rollback - if this SaveChangesAsync fails, the tenant
            // record remains in Provisioning status with no corresponding schema.
            // A background health-check job must detect and clean up stale
            // Provisioning records (tenants stuck in Provisioning > 10 minutes).
            _clinicRepository.Remove(clinic);
            _tenantRepository.Remove(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<OnboardClinicResponse>(provisioningResult.Error);
        }

        var provisionedResult = tenant.MarkProvisioned(provisioningResult.Value);
        if (provisionedResult.IsFailure)
        {
            return Result.Failure<OnboardClinicResponse>(provisionedResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OnboardClinicResponse.FromTenantAndClinic(tenant, clinic);
    }
}
