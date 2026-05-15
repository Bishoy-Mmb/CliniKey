using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class DentistRepository : IDentistRepository
{
    private readonly AppDbContext _dbContext;

    public DentistRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dentist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Dentist>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public void Add(Dentist dentist)
    {
        _dbContext.Set<Dentist>().Add(dentist);
    }
}
