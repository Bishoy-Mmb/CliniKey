using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Commands.ActivateTenant;

internal sealed class ActivateTenantCommandHandler : ICommandHandler<ActivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService,
        ITenantProvisioningService tenantProvisioningService,
        ITenantRegistry tenantRegistry,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _currentUserService = currentUserService;
        _tenantProvisioningService = tenantProvisioningService;
        _tenantRegistry = tenantRegistry;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.NotFound);
        }

        if (tenant.SchemaHealthStatus != TenantSchemaHealthStatus.Healthy)
        {
            return Result.Failure(TenantErrors.SchemaUnhealthy);
        }

        var activateResult = tenant.Activate();
        if (activateResult.IsFailure)
        {
            return activateResult;
        }

        Guid? operatorUserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _tenantRegistry.InvalidateAsync(tenant.Id, cancellationToken);
        await _tenantProvisioningService.RecordLifecycleAuditAsync(
            tenant,
            "Activate",
            "Succeeded",
            null,
            operatorUserId,
            cancellationToken);

        return Result.Success();
    }
}
