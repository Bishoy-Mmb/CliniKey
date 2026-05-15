using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _dbContext;

    public AppointmentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Appointment>()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> HasConflictAsync(Guid dentistId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Appointment>()
            .AnyAsync(a => a.DentistId == dentistId && 
                           a.Status != CliniKey.Domain.Enums.AppointmentStatus.Cancelled &&
                           a.StartTime < endTime && 
                           a.EndTime > startTime, 
                      cancellationToken);
    }

    public void Add(Appointment appointment)
    {
        _dbContext.Set<Appointment>().Add(appointment);
    }
}
