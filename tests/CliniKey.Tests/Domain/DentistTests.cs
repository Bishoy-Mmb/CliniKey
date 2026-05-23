using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace CliniKey.Tests.Domain;

public class DentistTests
{
    private readonly FakeTimeProvider _clock;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 21, 10, 0, 0, TimeSpan.Zero);

    public DentistTests()
    {
        _clock = new FakeTimeProvider(_fixedTime);
    }

    [Fact]
    public void Create_ValidInput_ReturnsDentist()
    {
        var result = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.FullName.Should().Be("Dr. Smith");
        result.Value.Specialization.Should().Be("General Dentistry");
        result.Value.LicenseNumber.Should().Be("LIC-1234");
        result.Value.CreatedAtUtc.Should().Be(_fixedTime.UtcDateTime);
    }

    [Fact]
    public void Create_EmptyFullName_ReturnsFailure()
    {
        var result = Dentist.Create(" ", "General Dentistry", "LIC-1234", _clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.InvalidFullName);
    }

    [Fact]
    public void Create_FullNameTooLong_ReturnsFailure()
    {
        var result = Dentist.Create(new string('a', Dentist.MaxFullNameLength + 1), "General Dentistry", "LIC-1234", _clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.FullNameTooLong);
    }

    [Fact]
    public void Create_EmptySpecialization_ReturnsFailure()
    {
        var result = Dentist.Create("Dr. Smith", " ", "LIC-1234", _clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.InvalidSpecialization);
    }

    [Fact]
    public void Create_SpecializationTooLong_ReturnsFailure()
    {
        var result = Dentist.Create("Dr. Smith", new string('a', Dentist.MaxSpecializationLength + 1), "LIC-1234", _clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.SpecializationTooLong);
    }

    [Fact]
    public void Create_EmptyLicenseNumber_ReturnsFailure()
    {
        var result = Dentist.Create("Dr. Smith", "General Dentistry", " ", _clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.InvalidLicenseNumber);
    }

    [Fact]
    public void Create_LicenseNumberTooLong_ReturnsFailure()
    {
        var result = Dentist.Create("Dr. Smith", "General Dentistry", new string('a', Dentist.MaxLicenseNumberLength + 1), _clock);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.LicenseNumberTooLong);
    }

    [Fact]
    public void Create_RaisesDomainEvent()
    {
        var result = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(e => e is DentistCreatedEvent);
    }

    [Fact]
    public void UpdateFullName_ValidInput_Succeeds()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;
        var updatedTime = _fixedTime.AddHours(1);
        _clock.SetUtcNow(updatedTime);

        var result = dentist.UpdateFullName("Dr. Ahmed");

        result.IsSuccess.Should().BeTrue();
        dentist.FullName.Should().Be("Dr. Ahmed");
        dentist.UpdatedAtUtc.Should().Be(updatedTime.UtcDateTime);
    }

    [Fact]
    public void UpdateFullName_SameValue_ReturnsSuccessWithoutMarkUpdated()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;

        var result = dentist.UpdateFullName("Dr. Smith");

        result.IsSuccess.Should().BeTrue();
        dentist.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void UpdateFullName_EmptyValue_ReturnsFailure()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;

        var result = dentist.UpdateFullName(" ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.InvalidFullName);
    }

    [Fact]
    public void UpdateFullName_TooLong_ReturnsFailure()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;

        var result = dentist.UpdateFullName(new string('a', Dentist.MaxFullNameLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.FullNameTooLong);
    }

    [Fact]
    public void UpdateSpecialization_ValidInput_Succeeds()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;
        var updatedTime = _fixedTime.AddHours(1);
        _clock.SetUtcNow(updatedTime);

        var result = dentist.UpdateSpecialization("Orthodontics");

        result.IsSuccess.Should().BeTrue();
        dentist.Specialization.Should().Be("Orthodontics");
        dentist.UpdatedAtUtc.Should().Be(updatedTime.UtcDateTime);
    }

    [Fact]
    public void UpdateSpecialization_SameValue_IsIdempotent()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;

        var result = dentist.UpdateSpecialization("General Dentistry");

        result.IsSuccess.Should().BeTrue();
        dentist.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void UpdateSpecialization_TooLongValue_ReturnsFailure()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;

        var result = dentist.UpdateSpecialization(new string('a', Dentist.MaxSpecializationLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.SpecializationTooLong);
    }

    [Fact]
    public void UpdateLicenseNumber_ValidInput_Succeeds()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;
        var updatedTime = _fixedTime.AddHours(1);
        _clock.SetUtcNow(updatedTime);

        var result = dentist.UpdateLicenseNumber("LIC-5678");

        result.IsSuccess.Should().BeTrue();
        dentist.LicenseNumber.Should().Be("LIC-5678");
        dentist.UpdatedAtUtc.Should().Be(updatedTime.UtcDateTime);
    }

    [Fact]
    public void UpdateLicenseNumber_TooLongValue_ReturnsFailure()
    {
        var dentist = Dentist.Create("Dr. Smith", "General Dentistry", "LIC-1234", _clock).Value;

        var result = dentist.UpdateLicenseNumber(new string('a', Dentist.MaxLicenseNumberLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DentistErrors.LicenseNumberTooLong);
    }
}
