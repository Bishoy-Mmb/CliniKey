using CliniKey.Domain.Entities;

namespace CliniKey.Domain.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid dentistId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    void Add(Appointment appointment);
}
