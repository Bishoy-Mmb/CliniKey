using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;

namespace CliniKey.Application.Features.Tenants.Queries.GetClinicById;

public sealed record GetClinicByIdQuery(Guid ClinicId) : IQuery<ClinicResponse>;
