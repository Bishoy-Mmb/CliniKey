using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Repositories;
using CliniKey.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class ClinicRepository : IClinicRepository
{
    private readonly AppDbContext _context;

    public ClinicRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Clinic>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Clinic?> GetPrimaryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Clinic>()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Clinic>> ListAsync(
        ClinicStatus? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(status)
            .OrderBy(c => c.Name)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Clinic>> ListAllAsync(
        ClinicStatus? status = null,
        IReadOnlyCollection<Guid>? clinicIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(status);

        if (clinicIds is not null)
        {
            query = query.Where(c => clinicIds.Contains(c.Id));
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        ClinicStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(status).CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPhoneAsync(
        PhoneNumber phone,
        Guid? excludingClinicId = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Clinic>()
            .AnyAsync(c => c.Phone == phone && (!excludingClinicId.HasValue || c.Id != excludingClinicId.Value), cancellationToken);
    }

    public void Add(Clinic clinic)
    {
        _context.Set<Clinic>().Add(clinic);
    }

    public void Remove(Clinic clinic)
    {
        _context.Set<Clinic>().Remove(clinic);
    }

    private IQueryable<Clinic> ApplyFilters(ClinicStatus? status)
    {
        var query = _context.Set<Clinic>().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return query;
    }
}
