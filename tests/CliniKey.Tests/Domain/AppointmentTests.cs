using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using FluentAssertions;
using Xunit;

namespace CliniKey.Tests.Domain;

public class AppointmentTests
{
    [Fact]
    public void Schedule_ValidInput_ReturnsAppointment()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var dentistId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(1);

        // Act
        var result = Appointment.Schedule(patientId, dentistId, startTime, endTime, "Checkup");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.PatientId.Should().Be(patientId);
        result.Value.DentistId.Should().Be(dentistId);
        result.Value.StartTime.Should().Be(startTime);
        result.Value.EndTime.Should().Be(endTime);
        result.Value.Status.Should().Be(AppointmentStatus.Scheduled);
        
        result.Value.DomainEvents.Should().ContainSingle(e => e is CliniKey.Domain.Events.AppointmentScheduledEvent);
    }

    [Fact]
    public void Schedule_PastDate_ReturnsFailure()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(-1);
        var endTime = startTime.AddHours(1);

        // Act
        var result = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), startTime, endTime);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AppointmentErrors.PastDate);
    }

    [Fact]
    public void CheckIn_FromScheduled_ChangesStatus()
    {
        // Arrange
        var appointment = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1)).Value;

        // Act
        var result = appointment.CheckIn();

        // Assert
        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.CheckedIn);
    }

    [Fact]
    public void CheckIn_FromCompleted_ReturnsFailure()
    {
        // Arrange
        var appointment = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1)).Value;
        appointment.CheckIn();
        appointment.Start();
        appointment.Complete();

        // Act
        var result = appointment.CheckIn();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AppointmentErrors.InvalidTransition);
    }

    [Fact]
    public void StateMachine_ValidTransitions()
    {
        // Arrange
        var appointment = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1)).Value;

        // Act & Assert
        appointment.CheckIn().IsSuccess.Should().BeTrue();
        appointment.Start().IsSuccess.Should().BeTrue();
        appointment.Complete().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Cancel_FromScheduled_ChangesStatus()
    {
        // Arrange
        var appointment = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1)).Value;

        // Act
        var result = appointment.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromCompleted_ReturnsFailure()
    {
        // Arrange
        var appointment = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1)).Value;
        appointment.CheckIn();
        appointment.Start();
        appointment.Complete();

        // Act
        var result = appointment.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AppointmentErrors.InvalidTransition);
    }

    [Fact]
    public void Cancel_FromCancelled_ReturnsFailure()
    {
        // Arrange
        var appointment = Appointment.Schedule(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1)).Value;
        appointment.Cancel();

        // Act
        var result = appointment.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AppointmentErrors.InvalidTransition);
    }
}
