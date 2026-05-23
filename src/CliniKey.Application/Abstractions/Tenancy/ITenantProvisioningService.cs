using CliniKey.Domain.Entities;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Abstractions.Tenancy;

public interface ITenantProvisioningService
{
    Task<Result<string?>> ProvisionAsync(
        Clinic clinic,
        Guid? operatorUserId,
        CancellationToken cancellationToken = default);

    Task RecordLifecycleAuditAsync(
        Clinic clinic,
        string operation,
        string status,
        string? message,
        Guid? operatorUserId,
        CancellationToken cancellationToken = default);
}
