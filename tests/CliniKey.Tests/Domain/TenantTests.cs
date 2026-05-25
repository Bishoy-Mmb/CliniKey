using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace CliniKey.Tests.Domain;

public class TenantTests
{
    private readonly FakeTimeProvider _clock;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 23, 10, 0, 0, TimeSpan.Zero);

    public TenantTests()
    {
        _clock = new FakeTimeProvider(_fixedTime);
    }

    [Fact]
    public void Create_ValidInput_ReturnsTenantWithPendingProvisioning()
    {
        var tenantId = Guid.NewGuid();
        var result = Tenant.Create(tenantId, "Cairo Dental Center", $"tenant_{tenantId:N}", _clock);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Cairo Dental Center");
        result.Value.SchemaName.Should().StartWith("tenant_");
        result.Value.SchemaName.Should().HaveLength(39);
        result.Value.SchemaName.Should().MatchRegex("^tenant_[0-9a-f]{32}$");
        Tenant.MaxSchemaNameLength.Should().BeGreaterThanOrEqualTo(result.Value.SchemaName.Length);
        result.Value.Status.Should().Be(TenantStatus.Active);
        result.Value.ProvisioningStatus.Should().Be(TenantProvisioningStatus.Pending);
        result.Value.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Unknown);
        result.Value.CreatedAtUtc.Should().Be(_fixedTime.UtcDateTime);
        var createdEvent = result.Value.DomainEvents.Should()
            .ContainSingle(e => e is TenantCreatedEvent)
            .Which.Should().BeOfType<TenantCreatedEvent>().Subject;
        createdEvent.TenantId.Should().Be(tenantId);
        createdEvent.SchemaName.Should().Be(result.Value.SchemaName);
        createdEvent.OccurredOnUtc.Should().Be(_fixedTime.UtcDateTime);
    }

    [Fact]
    public void MarkProvisioned_SetsHealthyStateAndRaisesEvent()
    {
        var tenant = CreateTenant();

        var result = tenant.MarkProvisioned("202605230001_InitialTenantOperationalSchema");

        result.IsSuccess.Should().BeTrue();
        tenant.ProvisioningStatus.Should().Be(TenantProvisioningStatus.Provisioned);
        tenant.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Healthy);
        tenant.CurrentMigration.Should().Be("202605230001_InitialTenantOperationalSchema");
        tenant.LastSchemaVerifiedAtUtc.Should().Be(_fixedTime.UtcDateTime);
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantProvisionedEvent);
    }

    [Fact]
    public void Deactivate_ActiveTenant_SetsInactiveAndRaisesEvent()
    {
        var tenant = CreateTenant();
        var operatorUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var result = tenant.Deactivate(operatorUserId);

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Inactive);
        tenant.IsActive.Should().BeFalse();
        tenant.DeactivatedAtUtc.Should().Be(_fixedTime.UtcDateTime);
        tenant.DeactivatedByUserId.Should().Be(operatorUserId);
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantDeactivatedEvent);
    }

    [Fact]
    public void Activate_InactiveHealthyTenant_SetsActiveAndRaisesEvent()
    {
        var tenant = CreateTenant();
        tenant.MarkProvisioned("202605230001_InitialTenantOperationalSchema");
        tenant.Deactivate(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        tenant.ClearDomainEvents();

        var result = tenant.Activate();

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.IsActive.Should().BeTrue();
        tenant.DeactivatedAtUtc.Should().BeNull();
        tenant.DeactivatedByUserId.Should().BeNull();
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantActivatedEvent);
    }

    [Fact]
    public void Activate_UnhealthyTenant_ReturnsSchemaUnhealthy()
    {
        var tenant = CreateTenant();
        tenant.Deactivate();

        var result = tenant.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.SchemaUnhealthy);
    }

    private Tenant CreateTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create(tenantId, "Cairo Dental Center", $"tenant_{tenantId:N}", _clock).Value;
        tenant.ClearDomainEvents();
        return tenant;
    }
}
