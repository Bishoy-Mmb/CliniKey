using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace CliniKey.Tests.Domain;

public class ClinicTests
{
    private readonly FakeTimeProvider _clock;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 23, 10, 0, 0, TimeSpan.Zero);

    public ClinicTests()
    {
        _clock = new FakeTimeProvider(_fixedTime);
    }

    [Fact]
    public void Create_ValidInput_ReturnsClinicWithPendingProvisioning()
    {
        var result = CreateClinicResult();

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Cairo Dental Center");
        result.Value.Phone.Value.Should().Be("01112345678");
        result.Value.Address.Should().Be("15 Tahrir St");
        result.Value.SchemaName.Should().StartWith("tenant_");
        result.Value.SchemaName.Should().HaveLength(39);
        result.Value.SchemaName.Should().MatchRegex("^tenant_[0-9a-f]{32}$");
        Clinic.MaxSchemaNameLength.Should().BeGreaterThanOrEqualTo(result.Value.SchemaName.Length);
        result.Value.Status.Should().Be(ClinicStatus.Active);
        result.Value.ProvisioningStatus.Should().Be(TenantProvisioningStatus.Pending);
        result.Value.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Unknown);
        result.Value.CreatedAtUtc.Should().Be(_fixedTime.UtcDateTime);
    }

    [Fact]
    public void Create_InvalidPhone_ReturnsFailure()
    {
        var result = CreateClinicResult(phone: "123");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PhoneNumber.InvalidFormat");
    }

    [Fact]
    public void Create_InvalidAddress_ReturnsFailure()
    {
        var result = CreateClinicResult(address: " ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ClinicErrors.InvalidAddress);
    }

    [Fact]
    public void MarkProvisioned_SetsHealthyStateAndRaisesEvent()
    {
        var clinic = CreateClinic();

        var result = clinic.MarkProvisioned("202605230001_InitialTenantOperationalSchema");

        result.IsSuccess.Should().BeTrue();
        clinic.ProvisioningStatus.Should().Be(TenantProvisioningStatus.Provisioned);
        clinic.SchemaHealthStatus.Should().Be(TenantSchemaHealthStatus.Healthy);
        clinic.CurrentMigration.Should().Be("202605230001_InitialTenantOperationalSchema");
        clinic.LastSchemaVerifiedAtUtc.Should().Be(_fixedTime.UtcDateTime);
        clinic.DomainEvents.Should().ContainSingle(e => e is ClinicProvisionedEvent);
    }

    [Fact]
    public void Deactivate_ActiveClinic_SetsInactiveAndRaisesEvent()
    {
        var clinic = CreateClinic();
        var operatorUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var result = clinic.Deactivate(operatorUserId);

        result.IsSuccess.Should().BeTrue();
        clinic.Status.Should().Be(ClinicStatus.Inactive);
        clinic.IsActive.Should().BeFalse();
        clinic.DeactivatedAtUtc.Should().Be(_fixedTime.UtcDateTime);
        clinic.DeactivatedByUserId.Should().Be(operatorUserId);
        clinic.DomainEvents.Should().ContainSingle(e => e is ClinicDeactivatedEvent);
    }

    [Fact]
    public void Deactivate_InactiveClinic_ReturnsAlreadyInactive()
    {
        var clinic = CreateClinic();
        clinic.Deactivate();

        var result = clinic.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ClinicErrors.AlreadyInactive);
    }

    [Fact]
    public void Activate_InactiveClinic_SetsActiveAndRaisesEvent()
    {
        var clinic = CreateClinic();
        clinic.Deactivate(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        clinic.ClearDomainEvents();

        var result = clinic.Activate();

        result.IsSuccess.Should().BeTrue();
        clinic.Status.Should().Be(ClinicStatus.Active);
        clinic.IsActive.Should().BeTrue();
        clinic.DeactivatedAtUtc.Should().BeNull();
        clinic.DeactivatedByUserId.Should().BeNull();
        clinic.DomainEvents.Should().ContainSingle(e => e is ClinicActivatedEvent);
    }

    [Fact]
    public void Activate_ActiveClinic_ReturnsAlreadyActive()
    {
        var clinic = CreateClinic();

        var result = clinic.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ClinicErrors.AlreadyActive);
    }

    [Fact]
    public void UpdateContact_ValidInput_ChangesPhoneAndAddress()
    {
        var clinic = CreateClinic();

        var result = clinic.UpdateContact("01198765432", "22 Nile Corniche");

        result.IsSuccess.Should().BeTrue();
        clinic.Phone.Value.Should().Be("01198765432");
        clinic.Address.Should().Be("22 Nile Corniche");
        clinic.DomainEvents.Should().ContainSingle(e => e is ClinicContactUpdatedEvent);
    }

    [Fact]
    public void UpdateContact_InvalidAddress_ReturnsFailureWithoutChangingPhone()
    {
        var clinic = CreateClinic();

        var result = clinic.UpdateContact("01198765432", " ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ClinicErrors.InvalidAddress);
        clinic.Phone.Value.Should().Be("01112345678");
        clinic.Address.Should().Be("15 Tahrir St");
    }

    private Clinic CreateClinic(
        string name = "Cairo Dental Center",
        string phone = "01112345678",
        string address = "15 Tahrir St")
    {
        return CreateClinicResult(name, phone, address).Value;
    }

    private Result<Clinic> CreateClinicResult(
        string name = "Cairo Dental Center",
        string phone = "01112345678",
        string address = "15 Tahrir St")
    {
        var clinicId = Guid.NewGuid();
        return Clinic.Create(clinicId, name, phone, address, $"tenant_{clinicId:N}", _clock);
    }
}
