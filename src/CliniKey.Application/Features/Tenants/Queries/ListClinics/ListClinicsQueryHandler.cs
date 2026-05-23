using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.ListClinics;

internal sealed class ListClinicsQueryHandler : IQueryHandler<ListClinicsQuery, ClinicListResponse>
{
    private readonly IClinicRepository _clinicRepository;

    public ListClinicsQueryHandler(IClinicRepository clinicRepository)
    {
        _clinicRepository = clinicRepository;
    }

    public async Task<Result<ClinicListResponse>> Handle(ListClinicsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var clinics = await _clinicRepository.ListAsync(
            request.Status,
            request.Health,
            page,
            pageSize,
            cancellationToken);
        var totalCount = await _clinicRepository.CountAsync(
            request.Status,
            request.Health,
            cancellationToken);

        return new ClinicListResponse(
            clinics.Select(ClinicListItemResponse.FromClinic).ToList().AsReadOnly(),
            page,
            pageSize,
            totalCount);
    }
}
