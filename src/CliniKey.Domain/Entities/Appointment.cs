using CliniKey.Domain.Enums;
using CliniKey.Domain.Events;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Appointment : AggregateRoot<Guid>, IAuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Appointment(Guid id, Guid patientId, Guid dentistId, DateTime startTime, DateTime endTime, string? notes)
    {
        Id = id;
        PatientId = patientId;
        DentistId = dentistId;
        StartTime = startTime;
        EndTime = endTime;
        Status = AppointmentStatus.Scheduled;
        Notes = notes;
    }

    private Appointment() { }

    public static Result<Appointment> Schedule(Guid patientId, Guid dentistId, DateTime startTime, DateTime endTime, string? notes = null)
    {
        if (startTime < DateTime.UtcNow)
        {
            return Result.Failure<Appointment>(AppointmentErrors.PastDate);
        }

        var appointment = new Appointment(Guid.NewGuid(), patientId, dentistId, startTime, endTime, notes);

        appointment.RaiseDomainEvent(new AppointmentScheduledEvent(appointment.Id, DateTime.UtcNow));

        return appointment;
    }

    public Result CheckIn()
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            return Result.Failure(AppointmentErrors.InvalidTransition);
        }

        ChangeStatus(AppointmentStatus.CheckedIn);
        return Result.Success();
    }

    public Result Start()
    {
        if (Status != AppointmentStatus.CheckedIn)
        {
            return Result.Failure(AppointmentErrors.InvalidTransition);
        }

        ChangeStatus(AppointmentStatus.InProgress);
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != AppointmentStatus.InProgress)
        {
            return Result.Failure(AppointmentErrors.InvalidTransition);
        }

        ChangeStatus(AppointmentStatus.Completed);
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == AppointmentStatus.Completed || Status == AppointmentStatus.Cancelled)
        {
            return Result.Failure(AppointmentErrors.InvalidTransition);
        }

        ChangeStatus(AppointmentStatus.Cancelled);
        return Result.Success();
    }

    private void ChangeStatus(AppointmentStatus newStatus)
    {
        var oldStatus = Status;
        Status = newStatus;
        RaiseDomainEvent(new AppointmentStatusChangedEvent(Id, oldStatus, newStatus, DateTime.UtcNow));
    }
}
