using CliniKey.Domain.Entities;
using CliniKey.Domain.ValueObjects;

namespace CliniKey.Domain.Repositories;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken = default);
    void Add(Patient patient);
}
