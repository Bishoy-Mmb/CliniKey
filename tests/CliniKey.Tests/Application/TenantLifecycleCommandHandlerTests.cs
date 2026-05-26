using CliniKey.Application.Abstractions.Identity;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Application.Features.Tenants.Commands.ActivateTenant;
using CliniKey.Application.Features.Tenants.Commands.DeactivateTenant;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CliniKey.Tests.Application;

public class TenantLifecycleCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ITenantRegistry _tenantRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FakeTimeProvider _clock;
    private readonly Guid _operatorUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public TenantLifecycleCommandHandlerTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _tenantProvisioningService = Substitute.For<ITenantProvisioningService>();
        _tenantRegistry = Substitute.For<ITenantRegistry>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));
        _currentUserService.UserId.Returns(_operatorUserId);
    }

    [Fact]
    public async Task DeactivateTenant_ActiveTenant_DeactivatesInvalidatesCacheAndAudits()
    {
        var (tenant, clinic) = CreateProvisionedTenantAndClinic();
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = new DeactivateTenantCommandHandler(
            _tenantRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(
            new DeactivateTenantCommand(tenant.Id, "Temporary closure"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Inactive);
        tenant.DeactivatedByUserId.Should().Be(_operatorUserId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(tenant.Id, Arg.Any<CancellationToken>());
        await _tenantProvisioningService.Received(1).RecordLifecycleAuditAsync(
            tenant,
            "Deactivate",
            "Succeeded",
            "Temporary closure",
            _operatorUserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateTenant_MissingTenant_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var handler = new DeactivateTenantCommandHandler(
            _tenantRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(new DeactivateTenantCommand(tenantId, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateTenant_HealthyInactiveTenant_ActivatesInvalidatesCacheAndAudits()
    {
        var (tenant, clinic) = CreateProvisionedTenantAndClinic();
        tenant.Deactivate(_operatorUserId);
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = new ActivateTenantCommandHandler(
            _tenantRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(new ActivateTenantCommand(tenant.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tenantRegistry.Received(1).InvalidateAsync(tenant.Id, Arg.Any<CancellationToken>());
        await _tenantProvisioningService.Received(1).RecordLifecycleAuditAsync(
            tenant,
            "Activate",
            "Succeeded",
            null,
            _operatorUserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateTenant_UnhealthyTenant_ReturnsSchemaUnhealthy()
    {
        var (tenant, clinic) = CreateProvisionedTenantAndClinic();
        tenant.MarkSchemaHealth(TenantSchemaHealthStatus.Unhealthy, tenant.CurrentMigration, _clock.GetUtcNow().UtcDateTime);
        tenant.Deactivate(_operatorUserId);
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = new ActivateTenantCommandHandler(
            _tenantRepository,
            _currentUserService,
            _tenantProvisioningService,
            _tenantRegistry,
            _unitOfWork);

        var result = await handler.Handle(new ActivateTenantCommand(tenant.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.SchemaUnhealthy);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private (Tenant Tenant, Clinic Clinic) CreateProvisionedTenantAndClinic()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create(
            tenantId,
            "Cairo Dental Center",
            $"tenant_{tenantId:N}",
            _clock).Value;
        tenant.MarkProvisioned("202605230001_InitialTenantOperationalSchema");
        tenant.ClearDomainEvents();

        var clinic = Clinic.Create(
            Guid.NewGuid(),
            tenant.Id,
            "Cairo Dental Center",
            "01112345678",
            "15 Tahrir St",
            _clock).Value;
        return (tenant, clinic);
    }
}
