using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.GetTenantById;

internal sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantResponse>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantResponse>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantResponse>(TenantErrors.NotFound);
        }

        var clinic = tenant.Clinics.OrderBy(c => c.CreatedAtUtc).FirstOrDefault();
        if (clinic is null)
        {
            return Result.Failure<TenantResponse>(ClinicErrors.NotFound);
        }

        return TenantResponse.FromTenantAndClinic(tenant, clinic);
    }
}
