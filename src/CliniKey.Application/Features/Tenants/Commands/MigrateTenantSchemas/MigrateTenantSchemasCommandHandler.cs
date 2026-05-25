using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;

internal sealed class MigrateTenantSchemasCommandHandler : ICommandHandler<MigrateTenantSchemasCommand, MigrateTenantSchemasResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMigrationService _tenantMigrationService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public MigrateTenantSchemasCommandHandler(
        ITenantRepository tenantRepository,
        ITenantMigrationService tenantMigrationService,
        ITenantRegistry tenantRegistry,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _tenantRepository = tenantRepository;
        _tenantMigrationService = tenantMigrationService;
        _tenantRegistry = tenantRegistry;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<MigrateTenantSchemasResponse>> Handle(
        MigrateTenantSchemasCommand request,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = _clock.GetUtcNow().UtcDateTime;
        TenantStatus? statusFilter = request.IncludeInactive ? null : TenantStatus.Active;
        var tenantIds = request.TenantIds?.ToHashSet();
        var tenants = await _tenantRepository.ListAllAsync(
            statusFilter,
            null,
            tenantIds,
            cancellationToken);
        var targets = tenants
            .Select(t => new TenantMigrationTarget(t.Id, t.SchemaName, request.IncludeInactive))
            .ToList();

        var migrationResult = await _tenantMigrationService.ApplyPendingMigrationsAsync(targets, cancellationToken);
        if (migrationResult.IsFailure)
        {
            return Result.Failure<MigrateTenantSchemasResponse>(migrationResult.Error);
        }

        var finishedAtUtc = _clock.GetUtcNow().UtcDateTime;
        foreach (var result in migrationResult.Value)
        {
            var tenant = tenants.First(t => t.Id == result.TenantId);
            var health = result.Status == "Succeeded"
                ? TenantSchemaHealthStatus.Healthy
                : TenantSchemaHealthStatus.Unhealthy;
            var markResult = tenant.MarkSchemaHealth(health, result.CurrentMigration ?? tenant.CurrentMigration, finishedAtUtc);
            if (markResult.IsFailure)
            {
                return Result.Failure<MigrateTenantSchemasResponse>(markResult.Error);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var result in migrationResult.Value)
        {
            await _tenantRegistry.InvalidateAsync(result.TenantId, cancellationToken);
        }

        return new MigrateTenantSchemasResponse(
            startedAtUtc,
            finishedAtUtc,
            _tenantMigrationService.ExpectedMigration,
            migrationResult.Value
                .Select(r => new TenantMigrationResultResponse(
                    r.TenantId,
                    r.SchemaName,
                    r.Status,
                    r.PreviousMigration,
                    r.CurrentMigration,
                    r.Message))
                .ToList()
                .AsReadOnly());
    }
}
