using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;

namespace CliniKey.Domain.Repositories;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Clinic?> GetPrimaryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> ListAsync(
        ClinicStatus? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> ListAllAsync(
        ClinicStatus? status = null,
        IReadOnlyCollection<Guid>? clinicIds = null,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        ClinicStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneAsync(PhoneNumber phone, Guid? excludingClinicId = null, CancellationToken cancellationToken = default);
    void Add(Clinic clinic);
    void Remove(Clinic clinic);
}
