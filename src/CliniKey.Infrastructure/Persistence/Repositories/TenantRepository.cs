using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;

    public TenantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Tenant>()
            .Include(t => t.Clinics)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetBySchemaNameAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Tenant>()
            .Include(t => t.Clinics)
            .FirstOrDefaultAsync(t => t.SchemaName == schemaName, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> ListAsync(
        TenantStatus? status = null,
        TenantSchemaHealthStatus? schemaHealthStatus = null,
        bool requireClinic = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(status, schemaHealthStatus, requireClinic)
            .Include(t => t.Clinics)
            .OrderBy(t => t.Name)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> ListAllAsync(
        TenantStatus? status = null,
        TenantSchemaHealthStatus? schemaHealthStatus = null,
        IReadOnlyCollection<Guid>? tenantIds = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Tenant> query = ApplyFilters(status, schemaHealthStatus).Include(t => t.Clinics);

        if (tenantIds is not null)
        {
            query = query.Where(t => tenantIds.Contains(t.Id));
        }

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        TenantStatus? status = null,
        TenantSchemaHealthStatus? schemaHealthStatus = null,
        bool requireClinic = false,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(status, schemaHealthStatus, requireClinic).CountAsync(cancellationToken);
    }

    public void Add(Tenant tenant)
    {
        _context.Set<Tenant>().Add(tenant);
    }

    public void Remove(Tenant tenant)
    {
        _context.Set<Tenant>().Remove(tenant);
    }

    private IQueryable<Tenant> ApplyFilters(
        TenantStatus? status,
        TenantSchemaHealthStatus? schemaHealthStatus,
        bool requireClinic = false)
    {
        var query = _context.Set<Tenant>().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (schemaHealthStatus.HasValue)
        {
            query = query.Where(t => t.SchemaHealthStatus == schemaHealthStatus.Value);
        }

        if (requireClinic)
        {
            query = query.Where(t => t.Clinics.Any());
        }

        return query;
    }
}
