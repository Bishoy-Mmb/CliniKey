using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Application.Features.Tenants.Commands.MigrateTenantSchemas;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CliniKey.Tests.Application;

public class TenantMigrationCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMigrationService _tenantMigrationService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;

    public TenantMigrationCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantMigrationService = Substitute.For<ITenantMigrationService>();
        _tenantRegistry = Substitute.For<ITenantRegistry>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
        _tenantMigrationService.ExpectedMigration.Returns("202605230001_InitialTenantOperationalSchema");
    }

    [Fact]
    public async Task Handle_UsesUnpagedFilteredTenantListForMigrationTargets()
    {
        var tenantIds = Enumerable.Range(0, 101)
            .Select(_ => Guid.NewGuid())
            .ToArray();
        var tenants = tenantIds
            .Select((_, index) => CreateTenant($"Practice {index}"))
            .ToList();
        _tenantRepository
            .ListAllAsync(
                TenantStatus.Active,
                null,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 101),
                Arg.Any<CancellationToken>())
            .Returns(tenants);
        _tenantMigrationService
            .ApplyPendingMigrationsAsync(Arg.Any<IReadOnlyCollection<TenantMigrationTarget>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var targets = call.Arg<IReadOnlyCollection<TenantMigrationTarget>>();
                return Result.Success<IReadOnlyList<TenantMigrationResult>>(
                    targets
                        .Select(t => new TenantMigrationResult(
                            t.TenantId,
                            t.SchemaName,
                            "Succeeded",
                            null,
                            "202605230001_InitialTenantOperationalSchema",
                            null))
                        .ToList()
                        .AsReadOnly());
            });
        var handler = new MigrateTenantSchemasCommandHandler(
            _tenantRepository,
            _tenantMigrationService,
            _tenantRegistry,
            _unitOfWork,
            _clock);

        var result = await handler.Handle(
            new MigrateTenantSchemasCommand(false, tenantIds),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Results.Should().HaveCount(101);
        await _tenantRegistry.Received(101).InvalidateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _tenantRepository.Received(1)
            .ListAllAsync(
                TenantStatus.Active,
                null,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 101),
                Arg.Any<CancellationToken>());
        await _tenantRepository.DidNotReceive()
            .ListAsync(
                Arg.Any<TenantStatus?>(),
                Arg.Any<TenantSchemaHealthStatus?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAnyTenantMigrationFails_MarksUnhealthyInvalidatesCacheAndReturnsPartialResults()
    {
        var successfulTenant = CreateTenant("Successful Practice");
        var failedTenant = CreateTenant("Failed Practice");
        _tenantRepository
            .ListAllAsync(TenantStatus.Active, null, null, Arg.Any<CancellationToken>())
            .Returns([successfulTenant, failedTenant]);
        _tenantMigrationService
            .ApplyPendingMigrationsAsync(Arg.Any<IReadOnlyCollection<TenantMigrationTarget>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<TenantMigrationResult>>(
                [
                    new TenantMigrationResult(
                        successfulTenant.Id,
                        successfulTenant.SchemaName,
                        "Succeeded",
                        successfulTenant.CurrentMigration,
                        "202605230001_InitialTenantOperationalSchema",
                        null),
                    new TenantMigrationResult(
                        failedTenant.Id,
                        failedTenant.SchemaName,
                        "Failed",
                        failedTenant.CurrentMigration,
                        null,
                        "Migration failed")
                ]));
        var handler = new MigrateTenantSchemasCommandHandler(
            _tenantRepository,
            _tenantMigrationService,
            _tenantRegistry,
            _unitOfWork,
            _clock);

        var result = await handler.Handle(new MigrateTenantSchemasCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Results.Should().ContainSingle(r =>
            r.TenantId == failedTenant.Id
            && r.Status == "Failed"
            && r.Message == "Migration failed");
        successfulTenant.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Healthy);
        failedTenant.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Unhealthy);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(successfulTenant.Id, Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(failedTenant.Id, Arg.Any<CancellationToken>());
    }

    private Tenant CreateTenant(string name)
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create(
            tenantId,
            name,
            $"tenant_{tenantId:N}",
            _clock).Value;
        tenant.MarkProvisioned("202605180001_PreviousMigration");
        tenant.ClearDomainEvents();
        return tenant;
    }
}
