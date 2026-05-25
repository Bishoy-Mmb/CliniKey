using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Commands.DeactivateTenant;

internal sealed class DeactivateTenantCommandHandler : ICommandHandler<DeactivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateTenantCommandHandler(
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

    public async Task<Result> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.NotFound);
        }

        Guid? operatorUserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
        var deactivateResult = tenant.Deactivate(operatorUserId);
        if (deactivateResult.IsFailure)
        {
            return deactivateResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _tenantRegistry.InvalidateAsync(tenant.Id, cancellationToken);
        await _tenantProvisioningService.RecordLifecycleAuditAsync(
            tenant,
            "Deactivate",
            "Succeeded",
            request.Reason,
            operatorUserId,
            cancellationToken);

        return Result.Success();
    }
}
