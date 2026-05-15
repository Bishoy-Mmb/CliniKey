using CliniKey.Domain.Entities;

namespace CliniKey.Domain.Repositories;

public interface IDentistRepository
{
    Task<Dentist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Dentist dentist);
}
