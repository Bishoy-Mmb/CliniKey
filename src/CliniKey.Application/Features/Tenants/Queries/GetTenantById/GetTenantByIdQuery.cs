using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Features.Tenants.Queries;

namespace CliniKey.Application.Features.Tenants.Queries.GetTenantById;

public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantResponse>;
