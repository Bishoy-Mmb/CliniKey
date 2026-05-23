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
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantMigrationService _tenantMigrationService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public MigrateTenantSchemasCommandHandler(
        IClinicRepository clinicRepository,
        ITenantMigrationService tenantMigrationService,
        ITenantRegistry tenantRegistry,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _clinicRepository = clinicRepository;
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
        ClinicStatus? statusFilter = request.IncludeInactive ? null : ClinicStatus.Active;
        var clinicIds = request.ClinicIds?.ToHashSet();
        var clinics = await _clinicRepository.ListAllAsync(
            statusFilter,
            null,
            clinicIds,
            cancellationToken);
        var targets = clinics
            .Select(c => new TenantMigrationTarget(c.Id, c.SchemaName, request.IncludeInactive))
            .ToList();

        var migrationResult = await _tenantMigrationService.ApplyPendingMigrationsAsync(targets, cancellationToken);
        if (migrationResult.IsFailure)
        {
            return Result.Failure<MigrateTenantSchemasResponse>(migrationResult.Error);
        }

        var finishedAtUtc = _clock.GetUtcNow().UtcDateTime;
        foreach (var result in migrationResult.Value)
        {
            var clinic = clinics.First(c => c.Id == result.ClinicId);
            var health = result.Status == "Succeeded"
                ? TenantSchemaHealthStatus.Healthy
                : TenantSchemaHealthStatus.Unhealthy;
            var markResult = clinic.MarkSchemaHealth(health, result.CurrentMigration ?? clinic.CurrentMigration, finishedAtUtc);
            if (markResult.IsFailure)
            {
                return Result.Failure<MigrateTenantSchemasResponse>(markResult.Error);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var result in migrationResult.Value)
        {
            await _tenantRegistry.InvalidateAsync(result.ClinicId, cancellationToken);
        }

        return new MigrateTenantSchemasResponse(
            startedAtUtc,
            finishedAtUtc,
            _tenantMigrationService.ExpectedMigration,
            migrationResult.Value
                .Select(r => new TenantMigrationResultResponse(
                    r.ClinicId,
                    r.SchemaName,
                    r.Status,
                    r.PreviousMigration,
                    r.CurrentMigration,
                    r.Message))
                .ToList()
                .AsReadOnly());
    }
}
