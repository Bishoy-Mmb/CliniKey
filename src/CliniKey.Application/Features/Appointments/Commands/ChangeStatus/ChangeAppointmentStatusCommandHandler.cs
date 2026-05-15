using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Repositories;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Application.Features.Appointments.Commands.ChangeStatus;

internal sealed class ChangeAppointmentStatusCommandHandler : ICommandHandler<ChangeAppointmentStatusCommand>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeAppointmentStatusCommandHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure(AppointmentErrors.NotFound);
        }

        Result result = request.NewStatus switch
        {
            AppointmentStatus.CheckedIn => appointment.CheckIn(),
            AppointmentStatus.InProgress => appointment.Start(),
            AppointmentStatus.Completed => appointment.Complete(),
            AppointmentStatus.Cancelled => appointment.Cancel(),
            _ => Result.Failure(AppointmentErrors.InvalidTransition)
        };

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
