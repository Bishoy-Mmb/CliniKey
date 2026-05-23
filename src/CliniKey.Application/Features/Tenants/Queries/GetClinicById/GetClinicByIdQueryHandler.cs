using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Queries.GetClinicById;

internal sealed class GetClinicByIdQueryHandler : IQueryHandler<GetClinicByIdQuery, ClinicResponse>
{
    private readonly IClinicRepository _clinicRepository;

    public GetClinicByIdQueryHandler(IClinicRepository clinicRepository)
    {
        _clinicRepository = clinicRepository;
    }

    public async Task<Result<ClinicResponse>> Handle(GetClinicByIdQuery request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure<ClinicResponse>(ClinicErrors.NotFound);
        }

        return ClinicResponse.FromClinic(clinic);
    }
}
