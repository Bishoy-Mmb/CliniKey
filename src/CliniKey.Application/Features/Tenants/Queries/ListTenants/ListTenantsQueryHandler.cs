using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.ListTenants;

internal sealed class ListTenantsQueryHandler : IQueryHandler<ListTenantsQuery, TenantListResponse>
{
    private readonly ITenantRepository _tenantRepository;

    public ListTenantsQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantListResponse>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var tenants = await _tenantRepository.ListAsync(
            request.Status,
            request.Health,
            requireClinic: true,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);
        var totalCount = await _tenantRepository.CountAsync(
            request.Status,
            request.Health,
            requireClinic: true,
            cancellationToken: cancellationToken);

        return new TenantListResponse(
            tenants
                .Select(TenantListItemResponse.FromTenant)
                .ToList()
                .AsReadOnly(),
            page,
            pageSize,
            totalCount);
    }
}
