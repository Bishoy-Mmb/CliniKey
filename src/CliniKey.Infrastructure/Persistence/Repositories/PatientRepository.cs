using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _dbContext;

    public PatientRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Patient patient)
    {
        _dbContext.Patients.Add(patient);
    }

    public async Task<bool> ExistsByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients.AnyAsync(p => p.Phone == phone, cancellationToken);
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
