using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class TreatmentPlanRepository(AppDbContext dbContext) : ITreatmentPlanRepository
{
    public async Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TreatmentPlan>()
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public void Add(TreatmentPlan treatmentPlan)
    {
        dbContext.Set<TreatmentPlan>().Add(treatmentPlan);
    }
}
