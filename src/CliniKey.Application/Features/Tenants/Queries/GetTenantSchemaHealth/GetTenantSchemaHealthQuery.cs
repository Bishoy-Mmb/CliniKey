using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Queries.GetTenantSchemaHealth;

public sealed record GetTenantSchemaHealthQuery : IQuery<TenantSchemaHealthResponse>;
