using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Commands.ActivateClinic;

internal sealed class ActivateClinicCommandHandler : ICommandHandler<ActivateClinicCommand>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateClinicCommandHandler(
        IClinicRepository clinicRepository,
        ICurrentUserService currentUserService,
        ITenantProvisioningService tenantProvisioningService,
        ITenantRegistry tenantRegistry,
        IUnitOfWork unitOfWork)
    {
        _clinicRepository = clinicRepository;
        _currentUserService = currentUserService;
        _tenantProvisioningService = tenantProvisioningService;
        _tenantRegistry = tenantRegistry;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ActivateClinicCommand request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure(ClinicErrors.NotFound);
        }

        if (clinic.SchemaHealthStatus != TenantSchemaHealthStatus.Healthy)
        {
            return Result.Failure(TenantErrors.SchemaUnhealthy);
        }

        var activateResult = clinic.Activate();
        if (activateResult.IsFailure)
        {
            return activateResult;
        }

        Guid? operatorUserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _tenantRegistry.InvalidateAsync(clinic.Id, cancellationToken);
        await _tenantProvisioningService.RecordLifecycleAuditAsync(
            clinic,
            "Activate",
            "Succeeded",
            null,
            operatorUserId,
            cancellationToken);

        return Result.Success();
    }
}
