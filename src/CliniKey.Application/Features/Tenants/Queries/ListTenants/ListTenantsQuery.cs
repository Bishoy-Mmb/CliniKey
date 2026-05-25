using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;
using CliniKey.Domain.Enums;

namespace CliniKey.Application.Features.Tenants.Queries.ListTenants;

public sealed record ListTenantsQuery(
    TenantStatus? Status,
    TenantSchemaHealthStatus? Health,
    int Page = 1,
    int PageSize = 50) : IQuery<TenantListResponse>;
