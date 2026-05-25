using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.GetClinicById;

internal sealed class GetClinicByIdQueryHandler : IQueryHandler<GetClinicByIdQuery, ClinicResponse>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantRepository _tenantRepository;

    public GetClinicByIdQueryHandler(IClinicRepository clinicRepository, ITenantRepository tenantRepository)
    {
        _clinicRepository = clinicRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<ClinicResponse>> Handle(GetClinicByIdQuery request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure<ClinicResponse>(ClinicErrors.NotFound);
        }

        var tenant = await _tenantRepository.GetByIdAsync(clinic.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<ClinicResponse>(TenantErrors.NotFound);
        }

        return ClinicResponse.FromTenantAndClinic(tenant, clinic);
    }
}
