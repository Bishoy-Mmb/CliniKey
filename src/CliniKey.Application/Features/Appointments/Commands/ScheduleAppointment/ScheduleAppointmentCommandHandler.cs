using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Entities;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Appointments.Commands.ScheduleAppointment;

internal sealed class ScheduleAppointmentCommandHandler : ICommandHandler<ScheduleAppointmentCommand, Guid>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ScheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        bool hasConflict = await _appointmentRepository.HasConflictAsync(request.DentistId, request.StartTime, request.EndTime, cancellationToken);
        if (hasConflict)
        {
            return Result.Failure<Guid>(AppointmentErrors.TimeConflict);
        }

        var appointmentResult = Appointment.Schedule(request.PatientId, request.DentistId, request.StartTime, request.EndTime, request.Notes);
        if (appointmentResult.IsFailure)
        {
            return Result.Failure<Guid>(appointmentResult.Error);
        }

        var appointment = appointmentResult.Value;
        _appointmentRepository.Add(appointment);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}
