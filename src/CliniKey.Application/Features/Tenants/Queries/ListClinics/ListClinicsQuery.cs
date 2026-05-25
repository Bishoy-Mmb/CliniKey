using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Enums;

namespace CliniKey.Application.Features.Tenants.Queries.ListClinics;

public sealed record ListClinicsQuery(
    TenantStatus? Status,
    TenantSchemaHealthStatus? Health,
    int Page = 1,
    int PageSize = 50) : IQuery<ClinicListResponse>;
