using CliniKey.Application.Features.Appointments.Commands.ScheduleAppointment;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CliniKey.Tests.Application;

public class ScheduleAppointmentCommandHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ScheduleAppointmentCommandHandler _handler;

    public ScheduleAppointmentCommandHandlerTests()
    {
        _appointmentRepositoryMock = Substitute.For<IAppointmentRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _handler = new ScheduleAppointmentCommandHandler(
            _appointmentRepositoryMock,
            _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_NoConflict_Succeeds()
    {
        // Arrange
        var command = new ScheduleAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "Test");

        _appointmentRepositoryMock.HasConflictAsync(command.DentistId, command.StartTime, command.EndTime, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _appointmentRepositoryMock.Received(1).Add(Arg.Any<Appointment>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OverlappingSlot_ReturnsTimeConflict()
    {
        // Arrange
        var command = new ScheduleAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
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
