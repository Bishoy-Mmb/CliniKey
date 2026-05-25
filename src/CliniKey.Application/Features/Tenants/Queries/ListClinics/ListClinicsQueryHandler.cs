using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.ListClinics;

internal sealed class ListClinicsQueryHandler : IQueryHandler<ListClinicsQuery, ClinicListResponse>
{
    private readonly ITenantRepository _tenantRepository;

    public ListClinicsQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<ClinicListResponse>> Handle(ListClinicsQuery request, CancellationToken cancellationToken)
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

        return new ClinicListResponse(
            tenants
                .Select(ClinicListItemResponse.FromTenant)
                .ToList()
                .AsReadOnly(),
            page,
            pageSize,
            totalCount);
    }
}
