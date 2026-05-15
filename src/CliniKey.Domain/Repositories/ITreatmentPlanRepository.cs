using CliniKey.Domain.Entities;

namespace CliniKey.Domain.Repositories;

public interface ITreatmentPlanRepository
{
    Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(TreatmentPlan treatmentPlan);
}
