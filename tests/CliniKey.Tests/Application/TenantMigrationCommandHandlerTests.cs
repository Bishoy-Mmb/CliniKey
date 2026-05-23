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
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantMigrationService _tenantMigrationService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;

    public TenantMigrationCommandHandlerTests()
    {
        _clinicRepository = Substitute.For<IClinicRepository>();
        _tenantMigrationService = Substitute.For<ITenantMigrationService>();
        _tenantRegistry = Substitute.For<ITenantRegistry>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
        _tenantMigrationService.ExpectedMigration.Returns("202605230001_InitialTenantOperationalSchema");
    }

    [Fact]
    public async Task Handle_UsesUnpagedFilteredClinicListForMigrationTargets()
    {
        var clinicIds = Enumerable.Range(0, 101)
            .Select(_ => Guid.NewGuid())
            .ToArray();
        var clinics = clinicIds
            .Select((_, index) => CreateClinic($"Clinic {index}"))
            .ToList();
        _clinicRepository
            .ListAllAsync(
                ClinicStatus.Active,
                null,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 101),
                Arg.Any<CancellationToken>())
            .Returns(clinics);
        _tenantMigrationService
            .ApplyPendingMigrationsAsync(Arg.Any<IReadOnlyCollection<TenantMigrationTarget>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var targets = call.Arg<IReadOnlyCollection<TenantMigrationTarget>>();
                return Result.Success<IReadOnlyList<TenantMigrationResult>>(
                    targets
                        .Select(t => new TenantMigrationResult(
                            t.ClinicId,
                            t.SchemaName,
                            "Succeeded",
                            null,
                            "202605230001_InitialTenantOperationalSchema",
                            null))
                        .ToList()
                        .AsReadOnly());
            });
        var handler = new MigrateTenantSchemasCommandHandler(
            _clinicRepository,
            _tenantMigrationService,
            _tenantRegistry,
            _unitOfWork,
            _clock);

        var result = await handler.Handle(
            new MigrateTenantSchemasCommand(false, clinicIds),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Results.Should().HaveCount(101);
        await _tenantRegistry.Received(101).InvalidateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _clinicRepository.Received(1)
            .ListAllAsync(
                ClinicStatus.Active,
                null,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 101),
                Arg.Any<CancellationToken>());
        await _clinicRepository.DidNotReceive()
            .ListAsync(Arg.Any<ClinicStatus?>(), Arg.Any<TenantSchemaHealthStatus?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAnyTenantMigrationFails_MarksUnhealthyInvalidatesCacheAndReturnsPartialResults()
    {
        var successfulClinic = CreateClinic("Successful Clinic");
        var failedClinic = CreateClinic("Failed Clinic");
        _clinicRepository
            .ListAllAsync(ClinicStatus.Active, null, null, Arg.Any<CancellationToken>())
            .Returns([successfulClinic, failedClinic]);
        _tenantMigrationService
            .ApplyPendingMigrationsAsync(Arg.Any<IReadOnlyCollection<TenantMigrationTarget>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<TenantMigrationResult>>(
                [
                    new TenantMigrationResult(
                        successfulClinic.Id,
                        successfulClinic.SchemaName,
                        "Succeeded",
                        successfulClinic.CurrentMigration,
                        "202605230001_InitialTenantOperationalSchema",
                        null),
                    new TenantMigrationResult(
                        failedClinic.Id,
                        failedClinic.SchemaName,
                        "Failed",
                        failedClinic.CurrentMigration,
                        null,
                        "Migration failed")
                ]));
        var handler = new MigrateTenantSchemasCommandHandler(
            _clinicRepository,
            _tenantMigrationService,
            _tenantRegistry,
            _unitOfWork,
            _clock);

        var result = await handler.Handle(new MigrateTenantSchemasCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Results.Should().ContainSingle(r =>
            r.ClinicId == failedClinic.Id
            && r.Status == "Failed"
            && r.Message == "Migration failed");
        successfulClinic.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Healthy);
        failedClinic.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Unhealthy);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(successfulClinic.Id, Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(failedClinic.Id, Arg.Any<CancellationToken>());
    }

    private Clinic CreateClinic(string name)
    {
        var clinic = Clinic.Create(name, "01112345678", "15 Tahrir St", _clock).Value;
        clinic.MarkProvisioned("202605180001_PreviousMigration");
        clinic.ClearDomainEvents();
        return clinic;
    }
}
