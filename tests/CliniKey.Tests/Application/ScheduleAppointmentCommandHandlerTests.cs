using CliniKey.Application.Features.Appointments.Commands.ScheduleAppointment;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Application;

public class ScheduleAppointmentCommandHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly FakeTimeProvider _clock;
    private readonly ScheduleAppointmentCommandHandler _handler;
    private readonly DateTimeOffset _fixedTime = new(2026, 5, 21, 10, 0, 0, TimeSpan.Zero);

    public ScheduleAppointmentCommandHandlerTests()
    {
        _appointmentRepositoryMock = Substitute.For<IAppointmentRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _clock = new FakeTimeProvider(_fixedTime);

        _handler = new ScheduleAppointmentCommandHandler(
            _appointmentRepositoryMock,
            _unitOfWorkMock,
            _clock);
    }

    [Fact]
    public async Task Handle_NoConflict_Succeeds()
    {
        // Arrange
        var command = new ScheduleAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _clock.GetUtcNow().UtcDateTime.AddDays(1),
            _clock.GetUtcNow().UtcDateTime.AddDays(1).AddHours(1),
            "Test");

        _appointmentRepositoryMock.HasConflictAsync(command.DentistId, command.StartTime, command.EndTime, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _appointmentRepositoryMock.Received(1).Add(Arg.Is<Appointment>(a =>
            a.PatientId == command.PatientId &&
            a.DentistId == command.DentistId &&
            a.StartTime == command.StartTime &&
            a.EndTime == command.EndTime));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OverlappingSlot_ReturnsTimeConflict()
    {
        // Arrange
        var command = new ScheduleAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _clock.GetUtcNow().UtcDateTime.AddDays(1),
            _clock.GetUtcNow().UtcDateTime.AddDays(1).AddHours(1),
            "Test");

        _appointmentRepositoryMock.HasConflictAsync(command.DentistId, command.StartTime, command.EndTime, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AppointmentErrors.TimeConflict);
        _appointmentRepositoryMock.DidNotReceive().Add(Arg.Any<Appointment>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
