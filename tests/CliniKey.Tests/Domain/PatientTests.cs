using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Events;
using CliniKey.Domain.ValueObjects;
using FluentAssertions;

namespace CliniKey.Tests.Domain;

public class PatientTests
{
    [Fact]
    public void Create_ValidInput_ReturnsPatient()
    {
        // Arrange
        var name = PatientName.Create("Ahmed", "Hassan").Value;
        var phone = PhoneNumber.Create("01012345678").Value;
        var dob = new DateOnly(1990, 1, 1);
        var gender = Gender.Male;

        // Act
        var patient = Patient.Create(name, phone, dob, gender);

        // Assert
        patient.Should().NotBeNull();
        patient.Name.Should().Be(name);
        patient.Phone.Should().Be(phone);
        patient.DateOfBirth.Should().Be(dob);
        patient.Gender.Should().Be(gender);
        patient.InsuranceDetails.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesDomainEvent()
    {
        // Arrange
        var name = PatientName.Create("Ahmed", "Hassan").Value;
        var phone = PhoneNumber.Create("01012345678").Value;

        // Act
        var patient = Patient.Create(name, phone, new DateOnly(1990, 1, 1), Gender.Male);

        // Assert
        var domainEvents = patient.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<PatientCreatedEvent>();
    }

    [Fact]
    public void UpdatePhone_ChangesPhone()
    {
        // Arrange
        var name = PatientName.Create("Ahmed", "Hassan").Value;
        var phone1 = PhoneNumber.Create("01012345678").Value;
        var phone2 = PhoneNumber.Create("01112345678").Value;
        var patient = Patient.Create(name, phone1, new DateOnly(1990, 1, 1), Gender.Male);

        // Act
        patient.UpdatePhone(phone2);

        // Assert
        patient.Phone.Should().Be(phone2);
    }

    [Fact]
    public void SoftDelete_SetsFlag()
    {
        // Arrange
        var name = PatientName.Create("Ahmed", "Hassan").Value;
        var phone = PhoneNumber.Create("01012345678").Value;
        var patient = Patient.Create(name, phone, new DateOnly(1990, 1, 1), Gender.Male);

        // Act
        patient.SoftDelete();

        // Assert
        patient.IsDeleted.Should().BeTrue();
        patient.DeletedAtUtc.Should().NotBeNull();
    }
}
