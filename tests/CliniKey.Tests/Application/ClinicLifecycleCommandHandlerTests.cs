using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Application.Features.Tenants.Commands.ActivateClinic;
using CliniKey.Application.Features.Tenants.Commands.DeactivateClinic;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CliniKey.Tests.Application;

public class ClinicLifecycleCommandHandlerTests
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;
    private readonly Guid _operatorUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public ClinicLifecycleCommandHandlerTests()
    {
        _clinicRepository = Substitute.For<IClinicRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _tenantProvisioningService = Substitute.For<ITenantProvisioningService>();
        _tenantRegistry = Substitute.For<ITenantRegistry>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
        _currentUserService.UserId.Returns(_operatorUserId);
    }

    [Fact]
    public async Task DeactivateClinic_ActiveClinic_DeactivatesInvalidatesCacheAndAudits()
    {
        var clinic = CreateProvisionedClinic();
        _clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var handler = new DeactivateClinicCommandHandler(
            _clinicRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(
            new DeactivateClinicCommand(clinic.Id, "Temporary closure"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clinic.Status.Should().Be(ClinicStatus.Inactive);
        clinic.DeactivatedByUserId.Should().Be(_operatorUserId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(clinic.Id, Arg.Any<CancellationToken>());
        await _tenantProvisioningService.Received(1).RecordLifecycleAuditAsync(
            clinic,
            "Deactivate",
            "Succeeded",
            "Temporary closure",
            _operatorUserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateClinic_MissingClinic_ReturnsNotFound()
    {
        var clinicId = Guid.NewGuid();
        var handler = new DeactivateClinicCommandHandler(
            _clinicRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(new DeactivateClinicCommand(clinicId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ClinicErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateClinic_HealthyInactiveClinic_ActivatesInvalidatesCacheAndAudits()
    {
        var clinic = CreateProvisionedClinic();
        clinic.Deactivate(_operatorUserId);
        _clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var handler = new ActivateClinicCommandHandler(
            _clinicRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(new ActivateClinicCommand(clinic.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clinic.Status.Should().Be(ClinicStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(clinic.Id, Arg.Any<CancellationToken>());
        await _tenantProvisioningService.Received(1).RecordLifecycleAuditAsync(
            clinic,
            "Activate",
            "Succeeded",
            null,
            _operatorUserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateClinic_UnhealthyClinic_ReturnsSchemaUnhealthy()
    {
        var clinic = CreateProvisionedClinic();
        clinic.MarkSchemaHealth(TenantSchemaHealthStatus.Unhealthy, clinic.CurrentMigration, _clock.GetUtcNow().UtcDateTime);
        clinic.Deactivate(_operatorUserId);
        _clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var handler = new ActivateClinicCommandHandler(
            _clinicRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(new ActivateClinicCommand(clinic.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.SchemaUnhealthy);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Clinic CreateProvisionedClinic()
    {
        var clinic = Clinic.Create("Cairo Dental Center", "01112345678", "15 Tahrir St", _clock).Value;
        clinic.MarkProvisioned("202605230001_InitialTenantOperationalSchema");
        clinic.ClearDomainEvents();
        return clinic;
    }
}
