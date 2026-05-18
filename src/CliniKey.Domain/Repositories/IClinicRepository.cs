using CliniKey.Domain.Entities;

namespace CliniKey.Domain.Repositories;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
