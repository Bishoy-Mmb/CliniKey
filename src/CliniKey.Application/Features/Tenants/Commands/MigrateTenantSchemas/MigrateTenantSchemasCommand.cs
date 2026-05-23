using CliniKey.Application.Abstractions.Messaging;

namespace CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;

public sealed record MigrateTenantSchemasCommand(
    bool IncludeInactive = false,
    IReadOnlyCollection<Guid>? ClinicIds = null) : ICommand<MigrateTenantSchemasResponse>;
